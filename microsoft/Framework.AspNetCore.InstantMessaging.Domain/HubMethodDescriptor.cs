using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubMethodDescriptor
    {
        /// <summary>
        /// 对象方法执行者
        /// </summary>
        public ObjectMethodExecutor MethodExecutor { get; }

        public Type NonAsyncReturnType { get; }

        public Type StreamReturnType { get; }

        private readonly MethodInfo _makeCancelableEnumerableMethodInfo;

        private static readonly MethodInfo MakeCancelableAsyncEnumerableMethod = typeof(AsyncEnumerableAdapters)
           .GetRuntimeMethods()
           .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeCancelableAsyncEnumerable)) && m.IsGenericMethod);

        private static readonly MethodInfo MakeAsyncEnumerableFromChannelMethod = typeof(AsyncEnumerableAdapters)
           .GetRuntimeMethods()
           .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeAsyncEnumerableFromChannel)) && m.IsGenericMethod);

        public IReadOnlyList<Type> ParameterTypes { get; }

        public bool IsStreamResponse => StreamReturnType != null;

        public bool HasSyntheticArguments { get; private set; }

        public List<Type> StreamingParameters { get; private set; }

        public IReadOnlyList<Type> OriginalParameterTypes { get; }

        public IList<IAuthorizeData> Policies { get; }

        public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IEnumerable<IAuthorizeData> policies)
        {
            MethodExecutor = methodExecutor;

            NonAsyncReturnType = (MethodExecutor.IsMethodAsync)
                ? MethodExecutor.AsyncResultType
                : MethodExecutor.MethodReturnType;

            foreach (var returnType in NonAsyncReturnType.GetInterfaces().Concat(NonAsyncReturnType.AllBaseTypes()))
            {
                if (!returnType.IsGenericType)
                {
                    continue;
                }

                var openReturnType = returnType.GetGenericTypeDefinition();

                if (openReturnType == typeof(IAsyncEnumerable<>))
                {
                    StreamReturnType = returnType.GetGenericArguments()[0];
                    _makeCancelableEnumerableMethodInfo = MakeCancelableAsyncEnumerableMethod;
                    break;
                }

                if (openReturnType == typeof(ChannelReader<>))
                {
                    StreamReturnType = returnType.GetGenericArguments()[0];
                    _makeCancelableEnumerableMethodInfo = MakeAsyncEnumerableFromChannelMethod;
                    break;
                }
            }

            // Take out synthetic arguments that will be provided by the server, this list will be given to the protocol parsers
            ParameterTypes = methodExecutor.MethodParameters.Where(p =>
            {
                // Only streams can take CancellationTokens currently
                if (IsStreamResponse && p.ParameterType == typeof(CancellationToken))
                {
                    HasSyntheticArguments = true;
                    return false;
                }
                else if (ReflectionHelper.IsStreamingType(p.ParameterType, mustBeDirectType: true))
                {
                    if (StreamingParameters == null)
                    {
                        StreamingParameters = new List<Type>();
                    }

                    StreamingParameters.Add(p.ParameterType.GetGenericArguments()[0]);
                    HasSyntheticArguments = true;
                    return false;
                }
                return true;
            }).Select(p => p.ParameterType).ToArray();

            if (HasSyntheticArguments)
            {
                OriginalParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
            }

            Policies = policies.ToArray();
        }
    }
}
