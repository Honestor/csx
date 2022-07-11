

using Framework.AspNetCore.Connections.Abstractions;
using Framework.AspNetCore.Connections.Common;
using Framework.AspNetCore.Connections.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections
{
    /// <summary>
    /// 连接调度
    /// </summary>
    internal partial class HttpConnectionDispatcher 
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private static readonly int _protocolVersion = 1;
        private readonly HttpConnectionManager _manager;
        public HttpConnectionDispatcher(ILoggerFactory loggerFactory, HttpConnectionManager manager)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
            _manager = manager;
        }


        #region Transport
        /// <summary>
        /// WebSocket
        /// </summary>
        private static readonly AvailableTransport _webSocketAvailableTransport =
          new AvailableTransport
          {
              Transport = nameof(HttpTransportType.WebSockets),
              TransferFormats = new List<string> { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
          };
        #endregion

        /// <summary>
        /// 协商
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task ExecuteNegotiateAsync(HttpContext context, HttpConnectionDispatcherOptions options)
        {
            var logScope = new ConnectionLogScope(connectionId: string.Empty);
            using (_logger.BeginScope(logScope))
            {
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    //必须是post才能进入协商
                    await ProcessNegotiate(context, options, logScope);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        /// <summary>
        /// 处理协商
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="logScope"></param>
        /// <returns></returns>
        private async Task ProcessNegotiate(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
        {
            context.Response.ContentType = "application/json";
            string error = null;
            int clientProtocolVersion = 0;
            if (context.Request.Query.TryGetValue("NegotiateVersion", out var queryStringVersion))
            {
                var queryStringVersionValue = queryStringVersion.ToString();
                if (!int.TryParse(queryStringVersionValue, out clientProtocolVersion))
                {
                    error = $"The client requested an invalid protocol version '{queryStringVersionValue}'";
                    Log.InvalidNegotiateProtocolVersion(_logger, queryStringVersionValue);
                }
                else if (clientProtocolVersion < options.MinimumProtocolVersion)
                {
                    error = $"The client requested version '{clientProtocolVersion}', but the server does not support this version.";
                    Log.NegotiateProtocolVersionMismatch(_logger, clientProtocolVersion);
                }
                else if (clientProtocolVersion > _protocolVersion)
                {
                    clientProtocolVersion = _protocolVersion;
                }
            }
            else if (options.MinimumProtocolVersion > 0)
            {
                error = $"The client requested version '0', but the server does not support this version.";
                Log.NegotiateProtocolVersionMismatch(_logger, 0);
            }

            HttpConnectionContext connection = null;
            if (error == null)
            {
                connection = CreateConnection(options, clientProtocolVersion);
            }

            logScope.ConnectionId = connection?.ConnectionId;

            var writer = new MemoryBufferWriter();

            try
            {
                WriteNegotiatePayload(writer, connection?.ConnectionId, connection?.ConnectionToken, context, options, clientProtocolVersion, error);

                Log.NegotiationRequest(_logger);

                // Write it out to the response with the right content length
                context.Response.ContentLength = writer.Length;
                await writer.CopyToAsync(context.Response.Body);
            }
            finally
            {
                writer.Reset();
            }
        }

        
        /// <summary>
        /// 写入协商的Payload
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="connectionId"></param>
        /// <param name="connectionToken"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="clientProtocolVersion"></param>
        /// <param name="error"></param>
        private void WriteNegotiatePayload(IBufferWriter<byte> writer, string connectionId, string connectionToken, HttpContext context, HttpConnectionDispatcherOptions options,
           int clientProtocolVersion, string error)
        {
            var response = new NegotiationResponse();

            if (!string.IsNullOrEmpty(error))
            {
                response.Error = error;
                NegotiateProtocol.WriteResponse(response, writer);
                return;
            }

            response.Version = clientProtocolVersion;
            response.ConnectionId = connectionId;
            response.ConnectionToken = connectionToken;
            response.AvailableTransports = new List<AvailableTransport>();

            if ((options.Transports & HttpTransportType.WebSockets) != 0 && ServerHasWebSockets(context.Features))
            {
                response.AvailableTransports.Add(_webSocketAvailableTransport);
            }
            NegotiateProtocol.WriteResponse(response, writer);
        }

        /// <summary>
        /// 服务是否支持WebSocket
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        private static bool ServerHasWebSockets(IFeatureCollection features)
        {
            return features.Get<IHttpWebSocketFeature>() != null;
        }

        private HttpConnectionContext CreateConnection(HttpConnectionDispatcherOptions options, int clientProtocolVersion = 0)
        {
            var transportPipeOptions = new PipeOptions(pauseWriterThreshold: options.TransportMaxBufferSize, resumeWriterThreshold: options.TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
            var appPipeOptions = new PipeOptions(pauseWriterThreshold: options.ApplicationMaxBufferSize, resumeWriterThreshold: options.ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
            return _manager.CreateConnection(transportPipeOptions, appPipeOptions, clientProtocolVersion);
        }

        /// <summary>
        /// 协商完成建立连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="connectionDelegate"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionDelegate connectionDelegate)
        {
            HttpConnectionContext connectionContext = null;
            var connectionToken = GetConnectionToken(context);
            if (connectionToken != null)
            {
                _manager.TryGetConnection(connectionToken, out connectionContext);
            }

            var logScope = new ConnectionLogScope(connectionContext?.ConnectionId);
            using (_logger.BeginScope(logScope))
            {
                if (HttpMethods.IsGet(context.Request.Method))
                {
                    await ExecuteAsync(context, connectionDelegate, options, logScope);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        private async Task ExecuteAsync(HttpContext context, ConnectionDelegate connectionDelegate, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
        {
            var supportedTransports = options.Transports;
            if (context.WebSockets.IsWebSocketRequest)
            {
                var connection = await GetOrCreateConnectionAsync(context, options);
                if (connection == null)
                {
                    return;
                }

                if (!await EnsureConnectionStateAsync(connection, context, HttpTransportType.WebSockets, supportedTransports, logScope, options))
                {
                    return;
                }

                Log.EstablishedConnection(_logger);

                //所有的read都可以被取消
                connection.Cancellation = new CancellationTokenSource();

                var ws = new WebSocketsServerTransport(options.WebSockets, connection.Application, connection, _loggerFactory);

                await DoPersistentConnection(connectionDelegate, ws, context, connection);
            }
        }

        /// <summary>
        /// 持久化连接
        /// </summary>
        /// <param name="connectionDelegate"></param>
        /// <param name="transport"></param>
        /// <param name="context"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private async Task DoPersistentConnection(ConnectionDelegate connectionDelegate,
                                          IHttpTransport transport,
                                          HttpContext context,
                                          HttpConnectionContext connection)
        {
            if (connection.TryActivatePersistentConnection(connectionDelegate, transport, _logger))
            {
                // Wait for any of them to end
                await Task.WhenAny(connection.ApplicationTask, connection.TransportTask);

                await _manager.DisposeAndRemoveAsync(connection, closeGracefully: true);
            }
        }

        private async Task<HttpConnectionContext> GetOrCreateConnectionAsync(HttpContext context, HttpConnectionDispatcherOptions options)
        {
            var connectionToken = GetConnectionToken(context);
            HttpConnectionContext connection;

            if (StringValues.IsNullOrEmpty(connectionToken))
            {
                connection = CreateConnection(options);
            }
            else if (!_manager.TryGetConnection(connectionToken, out connection))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connection;
        }

        private static string GetConnectionToken(HttpContext context) => context.Request.Query["id"];

        private async Task<bool> EnsureConnectionStateAsync(HttpConnectionContext connection, HttpContext context, HttpTransportType transportType, HttpTransportType supportedTransports, ConnectionLogScope logScope, HttpConnectionDispatcherOptions options)
        {
            if ((supportedTransports & transportType) == 0)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                Log.TransportNotSupported(_logger, transportType);
                await context.Response.WriteAsync($"{transportType} transport not supported by this end point type");
                return false;
            }

            connection.Features.Set(context.Features.Get<IHttpConnectionFeature>());

            if (connection.TransportType == HttpTransportType.None)
            {
                connection.TransportType = transportType;
            }
            else if (connection.TransportType != transportType)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.CannotChangeTransport(_logger, connection.TransportType, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }

            connection.HttpContext = context;
            connection.User = connection.HttpContext.User;
            logScope.ConnectionId = connection.ConnectionId;
            return true;
        }
    }
}
