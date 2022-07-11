using Framework.AspNetCore.Connections.Contexts;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections
{
    internal partial class HttpConnectionManager
    {
        private readonly ILogger<HttpConnectionManager> _logger;
        private readonly ILogger<HttpConnectionContext> _connectionLogger;
        private static readonly TimeSpan _heartbeatTickRate = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _disconnectTimeout;
        public HttpConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IOptions<ConnectionOptions> connectionOptions)
        {
            _logger = loggerFactory.CreateLogger<HttpConnectionManager>();
            _connectionLogger = loggerFactory.CreateLogger<HttpConnectionContext>();

            appLifetime.ApplicationStarted.Register(() => Start());
            _disconnectTimeout = connectionOptions.Value.DisconnectTimeout ?? ConnectionOptionsSetup.DefaultDisconectTimeout;
            _nextHeartbeat = new TimerAwaitable(_heartbeatTickRate, _heartbeatTickRate);
        }


        private readonly ConcurrentDictionary<string, (HttpConnectionContext Connection, ValueStopwatch Timer)> _connections =
            new ConcurrentDictionary<string, (HttpConnectionContext Connection, ValueStopwatch Timer)>(StringComparer.Ordinal);

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <returns></returns>
        internal HttpConnectionContext CreateConnection()
        {
            return CreateConnection(PipeOptions.Default, PipeOptions.Default);
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <param name="transportPipeOptions"></param>
        /// <param name="appPipeOptions"></param>
        /// <param name="negotiateVersion"></param>
        /// <returns></returns>
        internal HttpConnectionContext CreateConnection(PipeOptions transportPipeOptions, PipeOptions appPipeOptions, int negotiateVersion = 0)
        {
            string connectionToken;
            var id = MakeNewConnectionId();
            if (negotiateVersion > 0)
            {
                connectionToken = MakeNewConnectionId();
            }
            else
            {
                connectionToken = id;
            }

            Log.CreatedNewConnection(_logger, id);
            var connectionTimer = HttpConnectionsEventSource.Log.ConnectionStart(id);
            var connection = new HttpConnectionContext(id, connectionToken, _connectionLogger);
            var pair = DuplexPipe.CreateConnectionPair(transportPipeOptions, appPipeOptions);
            connection.Transport = pair.Application;
            connection.Application = pair.Transport;
            _connections.TryAdd(connectionToken, (connection, connectionTimer));
            return connection;
        }

        /// <summary>
        /// 尝试获取连接
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal bool TryGetConnection(string id, out HttpConnectionContext? connection)
        {
            connection = null;

            if (_connections.TryGetValue(id, out var pair))
            {
                connection = pair.Connection;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 生成连接Id
        /// </summary>
        /// <returns></returns>
        private static string MakeNewConnectionId()
        {
            //随机加密id
            Span<byte> buffer = stackalloc byte[16];
            RandomNumberGenerator.Fill(buffer);
            return WebEncoders.Base64UrlEncode(buffer);
        }

        /// <summary>
        /// 移除并释放连接和上下文
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="closeGracefully"></param>
        /// <returns></returns>
        internal async Task DisposeAndRemoveAsync(HttpConnectionContext connection, bool closeGracefully)
        {
            try
            {
                await connection.DisposeAsync(closeGracefully);
            }
            catch (IOException ex)
            {
                Log.ConnectionReset(_logger, connection.ConnectionId, ex);
            }
            catch (WebSocketException ex) when (ex.InnerException is IOException)
            {
                Log.ConnectionReset(_logger, connection.ConnectionId, ex);
            }
            catch (Exception ex)
            {
                Log.FailedDispose(_logger, connection.ConnectionId, ex);
            }
            finally
            {
                // Remove it from the list after disposal so that's it's easy to see
                // connections that might be in a hung state via the connections list
                RemoveConnection(connection.ConnectionToken);
            }
        }

        /// <summary>
        /// 移除连接
        /// </summary>
        /// <param name="id"></param>
        public void RemoveConnection(string id)
        {
            if (_connections.TryRemove(id, out var pair))
            {
                // Remove the connection completely
                HttpConnectionsEventSource.Log.ConnectionStop(id, pair.Timer);
                Log.RemovedConnection(_logger, id);
            }
        }

        private readonly TimerAwaitable _nextHeartbeat;
        public void Start()
        {
            _nextHeartbeat.Start();

            // Start the timer loop
            _ = ExecuteTimerLoop();
        }

        private async Task ExecuteTimerLoop()
        {
            Log.HeartBeatStarted(_logger);

            // Dispose the timer when all the code consuming callbacks has completed
            using (_nextHeartbeat)
            {
                // The TimerAwaitable will return true until Stop is called
                while (await _nextHeartbeat)
                {
                    try
                    {
                        Scan();
                    }
                    catch (Exception ex)
                    {
                        Log.ScanningConnectionsFailed(_logger, ex);
                    }
                }
            }

            Log.HeartBeatEnded(_logger);
        }

        public void Scan()
        {
            // Scan the registered connections looking for ones that have timed out
            foreach (var c in _connections)
            {
                var connection = c.Value.Connection;
                // Capture the connection state
                var lastSeenUtc = connection.LastSeenUtcIfInactive;

                var utcNow = DateTimeOffset.UtcNow;
                // Once the decision has been made to dispose we don't check the status again
                // But don't clean up connections while the debugger is attached.
                if (!Debugger.IsAttached && lastSeenUtc.HasValue && (utcNow - lastSeenUtc.Value).TotalSeconds > _disconnectTimeout.TotalSeconds)
                {
                    Log.ConnectionTimedOut(_logger, connection.ConnectionId);
                    HttpConnectionsEventSource.Log.ConnectionTimedOut(connection.ConnectionId);
                    _ = DisposeAndRemoveAsync(connection, closeGracefully: true);
                }
                else
                {
                    if (!Debugger.IsAttached)
                    {
                        connection.TryCancelSend(utcNow.Ticks);
                    }

                    connection.TickHeartbeat();
                }
            }
        }
    }
}
