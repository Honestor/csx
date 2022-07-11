using Framework.AspNetCore.Connections.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class HubConnectionHandler<THub> : ConnectionHandler where THub : Hub
    {
        internal ISystemClock SystemClock { get; set; } = new SystemClock();
        private readonly HubOptions<THub> _hubOptions;
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly HubOptions _globalHubOptions;
        private readonly long? _maximumMessageSize;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHubProtocolResolver _protocolResolver;
        private readonly IUserIdProvider _userIdProvider;
        private readonly bool _enableDetailedErrors;
        private readonly HubDispatcher<THub> _dispatcher;
        private readonly ILogger<HubConnectionHandler<THub>> _logger;
        public HubConnectionHandler(IOptions<HubOptions<THub>> hubOptions, IOptions<HubOptions> globalHubOptions, ILoggerFactory loggerFactory, IHubProtocolResolver protocolResolver, IUserIdProvider userIdProvider, HubLifetimeManager<THub> lifetimeManager, IServiceScopeFactory serviceScopeFactory)
        {
            _hubOptions = hubOptions.Value;
            _globalHubOptions = globalHubOptions.Value;

            List<IHubFilter>? hubFilters = null;
            if (_hubOptions.UserHasSetValues)
            {
                _enableDetailedErrors = _hubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
                _maximumMessageSize = _hubOptions.MaximumReceiveMessageSize;
            }
            else
            {
                _enableDetailedErrors = _globalHubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
                _maximumMessageSize = _globalHubOptions.MaximumReceiveMessageSize;
            }
            _loggerFactory = loggerFactory;
            _protocolResolver = protocolResolver;
            _userIdProvider = userIdProvider;
            _lifetimeManager = lifetimeManager;
            _logger = loggerFactory.CreateLogger<HubConnectionHandler<THub>>();

            
            _dispatcher = new DefaultHubDispatcher<THub>(
                serviceScopeFactory,
                new HubContext<THub>(lifetimeManager),
                _enableDetailedErrors,
                new Logger<DefaultHubDispatcher<THub>>(loggerFactory),
                hubFilters);
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var supportedProtocols = _hubOptions.SupportedProtocols ?? _globalHubOptions.SupportedProtocols;
            if (supportedProtocols == null || supportedProtocols.Count == 0)
            {
                throw new InvalidOperationException("There are no supported protocols");
            }
            //握手超时时间
            var handshakeTimeout = _hubOptions.HandshakeTimeout ?? _globalHubOptions.HandshakeTimeout ?? HubOptionsSetup.DefaultHandshakeTimeout;
            var contextOptions = new HubConnectionContextOptions()
            {
                KeepAliveInterval = _hubOptions.KeepAliveInterval ?? _globalHubOptions.KeepAliveInterval ?? HubOptionsSetup.DefaultKeepAliveInterval,
                ClientTimeoutInterval = _hubOptions.ClientTimeoutInterval ?? _globalHubOptions.ClientTimeoutInterval ?? HubOptionsSetup.DefaultClientTimeoutInterval,
                MaximumReceiveMessageSize = _maximumMessageSize,
                SystemClock = SystemClock
            };

            var connectionContext = new HubConnectionContext(connection, contextOptions, _loggerFactory);

            var resolvedSupportedProtocols = (supportedProtocols as IReadOnlyList<string>) ?? supportedProtocols.ToList();

            //握手,并建立心跳机制 长轮询除外
            if (!await connectionContext.HandshakeAsync(handshakeTimeout, resolvedSupportedProtocols, _protocolResolver, _userIdProvider, _enableDetailedErrors))
            {
                return;
            }

            try
            {
                //写入Hub连接上下文
                await _lifetimeManager.OnConnectedAsync(connectionContext);
                await RunHubAsync(connectionContext);
            }
            finally
            {
                connectionContext.Cleanup();

                Log.ConnectedEnding(_logger);
                //await _lifetimeManager.OnDisconnectedAsync(connectionContext);
            }
        }

        /// <summary>
        /// 运行Hub
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private async Task RunHubAsync(HubConnectionContext connection)
        {
            try
            {
                await _dispatcher.OnConnectedAsync(connection);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnConnectedAsync", ex);

                // 如果OnConnected中出现错误，客户端不应尝试重新连接。
                await SendCloseAsync(connection, ex, allowReconnect: false);

                return;
            }

            try
            {
                await DispatchMessagesAsync(connection);
            }
            catch (OperationCanceledException)
            {
                // Don't treat OperationCanceledException as an error, it's basically a "control flow"
                // exception to stop things from running
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingRequest(_logger, ex);

                await HubOnDisconnectedAsync(connection, ex);

                // return instead of throw to let close message send successfully
                return;
            }

            await HubOnDisconnectedAsync(connection, connection.CloseException);
        }

        /// <summary>
        /// 发送关闭信息
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="exception"></param>
        /// <param name="allowReconnect"></param>
        /// <returns></returns>
        private async Task SendCloseAsync(HubConnectionContext connection, Exception? exception, bool allowReconnect)
        {
            var closeMessage = CloseMessage.Empty;

            if (exception != null)
            {
                var errorMessage = ErrorMessageHelper.BuildErrorMessage("Connection closed with an error.", exception, _enableDetailedErrors);
                closeMessage = new CloseMessage(errorMessage, allowReconnect);
            }
            else if (allowReconnect)
            {
                closeMessage = new CloseMessage(error: null, allowReconnect);
            }

            try
            {
                await connection.WriteAsync(closeMessage, ignoreAbort: true);
            }
            catch (Exception ex)
            {
                Log.ErrorSendingClose(_logger, ex);
            }
        }


        private async Task DispatchMessagesAsync(HubConnectionContext connection)
        {
            var input = connection.Input;
            var protocol = connection.Protocol;
            connection.BeginClientTimeout();
            var binder = new HubConnectionBinder<THub>(_dispatcher, connection);
            while (true)
            {
                var result = await input.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    if (!buffer.IsEmpty)
                    {
                        bool messageReceived = false;
                        // No message limit, just parse and dispatch
                        if (_maximumMessageSize == null)
                        {
                            while (protocol.TryParseMessage(ref buffer, binder, out var message))
                            {
                                connection.StopClientTimeout();
                                // This lets us know the timeout has stopped and we need to re-enable it after dispatching the message
                                messageReceived = true;
                                await _dispatcher.DispatchMessageAsync(connection, message);
                            }

                            if (messageReceived)
                            {
                                connection.BeginClientTimeout();
                            }
                        }
                        else
                        {
                            // We give the parser a sliding window of the default message size
                            var maxMessageSize = _maximumMessageSize.Value;

                            while (!buffer.IsEmpty)
                            {
                                var segment = buffer;
                                var overLength = false;

                                if (segment.Length > maxMessageSize)
                                {
                                    segment = segment.Slice(segment.Start, maxMessageSize);
                                    overLength = true;
                                }

                                if (protocol.TryParseMessage(ref segment, binder, out var message))
                                {
                                    if (!(message is PingMessage))
                                    {
                                        connection.StopClientTimeout();
                                        // This lets us know the timeout has stopped and we need to re-enable it after dispatching the message
                                        messageReceived = true;
                                        await _dispatcher.DispatchMessageAsync(connection, message);
                                    }
                                }
                                else if (overLength)
                                {
                                    throw new InvalidDataException($"The maximum message size of {maxMessageSize}B was exceeded. The message size can be configured in AddHubOptions.");
                                }
                                else
                                {
                                    // No need to update the buffer since we didn't parse anything
                                    break;
                                }

                                // Update the buffer to the remaining segment
                                buffer = buffer.Slice(segment.Start);
                            }

                            if (messageReceived)
                            {
                                connection.BeginClientTimeout();
                            }
                        }
                    }

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                        {
                            throw new InvalidDataException("Connection terminated while reading a message.");
                        }
                        break;
                    }
                }
                finally
                {
                    // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                    // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                    // before yielding the read again.
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        private async Task HubOnDisconnectedAsync(HubConnectionContext connection, Exception? exception)
        {
            // 在中止连接之前发送关闭消息
            await SendCloseAsync(connection, exception, connection.AllowReconnect);

            // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
            // before OnDisconnectedAsync

            // Ensure the connection is aborted before firing disconnect
            await connection.AbortAsync();

            try
            {
                await _dispatcher.OnDisconnectedAsync(connection, exception);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnDisconnectedAsync", ex);
                throw;
            }
        }

        #region 日志
        private static class Log
        {
            private static readonly Action<ILogger, string, Exception?> _errorDispatchingHubEvent =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "ErrorDispatchingHubEvent"), "Error when dispatching '{HubMethod}' on hub.");

            private static readonly Action<ILogger, Exception?> _errorProcessingRequest =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "ErrorProcessingRequest"), "Error when processing requests.");

            private static readonly Action<ILogger, Exception?> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, Exception?> _errorSendingClose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ErrorSendingClose"), "Error when sending Close message.");

            private static readonly Action<ILogger, Exception?> _connectedStarting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ConnectedStarting"), "OnConnectedAsync started.");

            private static readonly Action<ILogger, Exception?> _connectedEnding =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "ConnectedEnding"), "OnConnectedAsync ending.");

            public static void ErrorDispatchingHubEvent(ILogger logger, string hubMethod, Exception exception)
            {
                _errorDispatchingHubEvent(logger, hubMethod, exception);
            }

            public static void ErrorProcessingRequest(ILogger logger, Exception exception)
            {
                _errorProcessingRequest(logger, exception);
            }

            public static void AbortFailed(ILogger logger, Exception exception)
            {
                _abortFailed(logger, exception);
            }

            public static void ErrorSendingClose(ILogger logger, Exception exception)
            {
                _errorSendingClose(logger, exception);
            }

            public static void ConnectedStarting(ILogger logger)
            {
                _connectedStarting(logger, null);
            }

            public static void ConnectedEnding(ILogger logger)
            {
                _connectedEnding(logger, null);
            }
        }
        #endregion
    }
}
