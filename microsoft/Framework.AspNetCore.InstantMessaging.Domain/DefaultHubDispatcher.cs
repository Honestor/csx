using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal partial class DefaultHubDispatcher<THub> : HubDispatcher<THub> where THub : Hub
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<HubDispatcher<THub>> _logger;
        private readonly Func<HubLifetimeContext, Task> _onConnectedMiddleware;
        private readonly Func<HubLifetimeContext, Exception, Task> _onDisconnectedMiddleware;
        private readonly Func<HubInvocationContext, ValueTask<object>> _invokeMiddleware;
        private readonly bool _enableDetailedErrors;

        public DefaultHubDispatcher(IServiceScopeFactory serviceScopeFactory, IHubContext<THub> hubContext, bool enableDetailedErrors,
             ILogger<DefaultHubDispatcher<THub>> logger, List<IHubFilter> hubFilters)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _logger = logger;
            _enableDetailedErrors = enableDetailedErrors;
            DiscoverHubMethods();
        }

        /// <summary>
        /// 消息按顺序发送，并将停止处理其他消息，直到它们完成。
        /// 启用并行调用后，消息将按顺序运行，直到它们变为异步，然后允许下一条消息开始运行。
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="hubMessage"></param>
        /// <returns></returns>
        public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            switch (hubMessage)
            {
                case InvocationBindingFailureMessage bindingFailureMessage:
                    return ProcessInvocationBindingFailure(connection, bindingFailureMessage);
                case InvocationMessage invocationMessage:
                    Log.ReceivedHubInvocation(_logger, invocationMessage);
                    return ProcessInvocation(connection, invocationMessage, isStreamResponse: false);
                case PingMessage _:
                    connection.StartClientTimeout();
                    break;
                default:
                    Log.UnsupportedMessageReceived(_logger, hubMessage.GetType().FullName);
                    throw new NotSupportedException($"Received unsupported message: {hubMessage}");
            }

            return Task.CompletedTask;
        }


        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);
        public override IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            if (!_methods.TryGetValue(methodName, out var descriptor))
            {
                throw new HubException("Method does not exist.");
            }
            return descriptor.ParameterTypes;
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            IServiceScope scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();
                try
                {
                    InitializeHub(hub, connection);

                    if (_onConnectedMiddleware != null)
                    {
                        var context = new HubLifetimeContext(connection.HubCallerContext, scope.ServiceProvider, hub);
                        await _onConnectedMiddleware(context);
                    }
                    else
                    {
                        await hub.OnConnectedAsync();
                    }
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
            finally
            {
                await scope.DisposeAsync();
            }
        }

        private void InitializeHub(THub hub, HubConnectionContext connection)
        {
            hub.Clients = new HubCallerClients(_hubContext.Clients, connection.ConnectionId);
            hub.Context = connection.HubCallerContext;
            hub.Groups = _hubContext.Groups;
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            IServiceScope scope = null;

            try
            {
                scope = _serviceScopeFactory.CreateScope();

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();
                try
                {
                    InitializeHub(hub, connection);

                    if (_onDisconnectedMiddleware != null)
                    {
                        var context = new HubLifetimeContext(connection.HubCallerContext, scope.ServiceProvider, hub);
                        await _onDisconnectedMiddleware(context, exception);
                    }
                    else
                    {
                        await hub.OnDisconnectedAsync(exception);
                    }
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
            finally
            {
                await scope.DisposeAsync();
            }
        }

        /// <summary>
        /// 查找hub所有的method并缓存到_methods字典中
        /// </summary>
        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();
            var hubName = hubType.Name;

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
                if (methodInfo.IsGenericMethod)
                {
                    throw new NotSupportedException($"Method '{methodInfo.Name}' is a generic method which is not supported on a Hub.");
                }

                var methodName =
                    methodInfo.GetCustomAttribute<HubMethodNameAttribute>()?.Name ??
                    methodInfo.Name;

                if (_methods.ContainsKey(methodName))
                {
                    throw new NotSupportedException($"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubTypeInfo);
                var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                _methods[methodName] = new HubMethodDescriptor(executor, authorizeAttributes);

                Log.HubMethodBound(_logger, hubName, methodName);
            }
        }

        private Task ProcessInvocationBindingFailure(HubConnectionContext connection, InvocationBindingFailureMessage bindingFailureMessage)
        {
            Log.InvalidHubParameters(_logger, bindingFailureMessage.Target, bindingFailureMessage.BindingFailure.SourceException);

            var errorMessage = ErrorMessageHelper.BuildErrorMessage($"Failed to invoke '{bindingFailureMessage.Target}' due to an error on the server.",
                bindingFailureMessage.BindingFailure.SourceException, _enableDetailedErrors);
            return SendInvocationError(bindingFailureMessage.InvocationId, connection, errorMessage);
        }

        private async Task SendInvocationError(string invocationId,
          HubConnectionContext connection, string errorMessage)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                return;
            }

            await connection.WriteAsync(CompletionMessage.WithError(invocationId, errorMessage));
        }

        private Task ProcessInvocation(HubConnectionContext connection,
           HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamResponse)
        {
            if (!_methods.TryGetValue(hubMethodInvocationMessage.Target, out var descriptor))
            {
                // Send an error to the client. Then let the normal completion process occur
                Log.UnknownHubMethod(_logger, hubMethodInvocationMessage.Target);
                return connection.WriteAsync(CompletionMessage.WithError(
                    hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'")).AsTask();
            }
            else
            {
                bool isStreamCall = descriptor.StreamingParameters != null;
                if (connection.ActiveInvocationLimit != null && !isStreamCall && !isStreamResponse)
                {
                    return connection.ActiveInvocationLimit.RunAsync(state =>
                    {
                        var (dispatcher, descriptor, connection, invocationMessage) = state;
                        return dispatcher.Invoke(descriptor, connection, invocationMessage, isStreamResponse: false, isStreamCall: false);
                    }, (this, descriptor, connection, hubMethodInvocationMessage));
                }
                else
                {
                    return Invoke(descriptor, connection, hubMethodInvocationMessage, isStreamResponse, isStreamCall);
                }
            }
        }

        private async Task Invoke(HubMethodDescriptor descriptor, HubConnectionContext connection,
         HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamResponse, bool isStreamCall)
        {
            var methodExecutor = descriptor.MethodExecutor;

            var disposeScope = true;
            var scope = _serviceScopeFactory.CreateScope();
            IHubActivator<THub> hubActivator = null;
            THub hub = null;
            try
            {
                hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                hub = hubActivator.Create();

                if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection, descriptor, hubMethodInvocationMessage.Arguments, hub))
                {
                    Log.HubMethodNotAuthorized(_logger, hubMethodInvocationMessage.Target);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        $"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
                    return;
                }

                if (!await ValidateInvocationMode(descriptor, isStreamResponse, hubMethodInvocationMessage, connection))
                {
                    return;
                }

                try
                {
                    var clientStreamLength = hubMethodInvocationMessage.StreamIds?.Length ?? 0;
                    var serverStreamLength = descriptor.StreamingParameters?.Count ?? 0;
                    if (clientStreamLength != serverStreamLength)
                    {
                        var ex = new HubException($"Client sent {clientStreamLength} stream(s), Hub method expects {serverStreamLength}.");
                        Log.InvalidHubParameters(_logger, hubMethodInvocationMessage.Target, ex);
                        await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                            ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                        return;
                    }

                    InitializeHub(hub, connection);
                    Task invocation = null;

                    var arguments = hubMethodInvocationMessage.Arguments;
                    CancellationTokenSource cts = null;
                    if (descriptor.HasSyntheticArguments)
                    {
                        ReplaceArguments(descriptor, hubMethodInvocationMessage, isStreamCall, connection, ref arguments, out cts);
                    }

                    if (isStreamResponse)
                    {
                        //_ = StreamAsync(hubMethodInvocationMessage.InvocationId, connection, arguments, scope, hubActivator, hub, cts, hubMethodInvocationMessage, descriptor);
                    }
                    else
                    {
                        // Invoke or Send
                        async Task ExecuteInvocation()
                        {
                            object result;
                            try
                            {
                                result = await ExecuteHubMethod(methodExecutor, hub, arguments, connection, scope.ServiceProvider);
                                Log.SendingResult(_logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                            }
                            catch (Exception ex)
                            {
                                Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                                await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                                    ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                                return;
                            }
                            finally
                            {
                                // Stream response handles cleanup in StreamResultsAsync
                                // And normal invocations handle cleanup below in the finally
                                if (isStreamCall)
                                {
                                    await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);
                                }
                            }

                            // No InvocationId - Send Async, no response expected
                            if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
                            {
                                // Invoke Async, one reponse expected
                                await connection.WriteAsync(CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                            }
                        }
                        invocation = ExecuteInvocation();
                    }

                    if (isStreamCall || isStreamResponse)
                    {
                        // don't await streaming invocations
                        // leave them running in the background, allowing dispatcher to process other messages between streaming items
                        disposeScope = false;
                    }
                    else
                    {
                        // complete the non-streaming calls now
                        await invocation;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex.InnerException, _enableDetailedErrors));
                }
                catch (Exception ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                }
            }
            finally
            {
                if (disposeScope)
                {
                    await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);
                }
            }
        }

        private ValueTask CleanupInvocation(HubConnectionContext connection, HubMethodInvocationMessage hubMessage, IHubActivator<THub> hubActivator,
           THub hub, IServiceScope scope)
        {
            if (hubMessage.StreamIds != null)
            {
                //foreach (var stream in hubMessage.StreamIds)
                //{
                //    connection.StreamTracker.TryComplete(CompletionMessage.Empty(stream));
                //}
            }

            hubActivator?.Release(hub);

            return scope.DisposeAsync();
        }

        private Task<bool> IsHubMethodAuthorized(IServiceProvider provider, HubConnectionContext hubConnectionContext, HubMethodDescriptor descriptor, object[] hubMethodArguments, Hub hub)
        {
            if (descriptor.Policies.Count == 0)
            {
                return TaskCache.True;
            }

            return IsHubMethodAuthorizedSlow(provider, hubConnectionContext.User, descriptor.Policies, new HubInvocationContext(hubConnectionContext.HubCallerContext, provider, hub, descriptor.MethodExecutor.MethodInfo, hubMethodArguments));
        }


        private static async Task<bool> IsHubMethodAuthorizedSlow(IServiceProvider provider, ClaimsPrincipal principal, IList<IAuthorizeData> policies, HubInvocationContext resource)
        {
            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, resource, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private async Task<bool> ValidateInvocationMode(HubMethodDescriptor hubMethodDescriptor, bool isStreamResponse,
            HubMethodInvocationMessage hubMethodInvocationMessage, HubConnectionContext connection)
        {
            if (hubMethodDescriptor.IsStreamResponse && !isStreamResponse)
            {
                // Non-null/empty InvocationId? Blocking
                if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
                {
                    Log.StreamingMethodCalledWithInvoke(_logger, hubMethodInvocationMessage);
                    await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                        $"The client attempted to invoke the streaming '{hubMethodInvocationMessage.Target}' method with a non-streaming invocation."));
                }

                return false;
            }

            if (!hubMethodDescriptor.IsStreamResponse && isStreamResponse)
            {
                Log.NonStreamingMethodCalledWithStream(_logger, hubMethodInvocationMessage);
                await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method with a streaming invocation."));

                return false;
            }

            return true;
        }

        private void ReplaceArguments(HubMethodDescriptor descriptor, HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamCall,
          HubConnectionContext connection, ref object[] arguments, out CancellationTokenSource cts)
        {
            cts = null;
            // In order to add the synthetic arguments we need a new array because the invocation array is too small (it doesn't know about synthetic arguments)
            arguments = new object[descriptor.OriginalParameterTypes.Count];

            //var streamPointer = 0;
            var hubInvocationArgumentPointer = 0;
            for (var parameterPointer = 0; parameterPointer < arguments.Length; parameterPointer++)
            {
                if (hubMethodInvocationMessage.Arguments.Length > hubInvocationArgumentPointer &&
                    (hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer] == null ||
                    descriptor.OriginalParameterTypes[parameterPointer].IsAssignableFrom(hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer].GetType())))
                {
                    // The types match so it isn't a synthetic argument, just copy it into the arguments array
                    arguments[parameterPointer] = hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer];
                    hubInvocationArgumentPointer++;
                }
                else
                {
                    if (descriptor.OriginalParameterTypes[parameterPointer] == typeof(CancellationToken))
                    {
                        cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);
                        arguments[parameterPointer] = cts.Token;
                    }
                    else if (isStreamCall && ReflectionHelper.IsStreamingType(descriptor.OriginalParameterTypes[parameterPointer], mustBeDirectType: true))
                    {
                        //Log.StartingParameterStream(_logger, hubMethodInvocationMessage.StreamIds[streamPointer]);
                        //var itemType = descriptor.StreamingParameters[streamPointer];
                        //arguments[parameterPointer] = connection.StreamTracker.AddStream(hubMethodInvocationMessage.StreamIds[streamPointer],
                        //    itemType, descriptor.OriginalParameterTypes[parameterPointer]);

                        //streamPointer++;
                    }
                    else
                    {
                        // This should never happen
                        Debug.Assert(false, $"Failed to bind argument of type '{descriptor.OriginalParameterTypes[parameterPointer].Name}' for hub method '{descriptor.MethodExecutor.MethodInfo.Name}'.");
                    }
                }
            }
        }

        private ValueTask<object> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub, object[] arguments, HubConnectionContext connection, IServiceProvider serviceProvider)
        {
            if (_invokeMiddleware != null)
            {
                var invocationContext = new HubInvocationContext(methodExecutor, connection.HubCallerContext, serviceProvider, hub, arguments);
                return _invokeMiddleware(invocationContext);
            }

            // If no Hub filters are registered
            return ExecuteMethod(methodExecutor, hub, arguments);
        }

        private async ValueTask<object> ExecuteMethod(ObjectMethodExecutor methodExecutor, Hub hub, object[] arguments)
        {
            if (methodExecutor.IsMethodAsync)
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task)methodExecutor.Execute(hub, arguments);
                    return null;
                }
                else
                {
                    return await methodExecutor.ExecuteAsync(hub, arguments);
                }
            }
            else
            {
                return methodExecutor.Execute(hub, arguments);
            }
        }
    }
}
