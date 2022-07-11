using Framework.AspNetCore.Connections.Abstractions;
using Framework.AspNetCore.Connections.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Contexts
{
    internal class HttpConnectionContext: ConnectionContext,
        IConnectionInherentKeepAliveFeature,
        IConnectionHeartbeatFeature,
        IConnectionLifetimeFeature,
        IConnectionUserFeature,
        ITransferFormatFeature,
        IHttpContextFeature
    {
        /// <summary>
        /// 连接Id
        /// </summary>
        public override string ConnectionId { get; set; }

        /// <summary>
        /// 连接Token
        /// </summary>
        internal string ConnectionToken { get; set; }

        /// <summary>
        /// Feature集合
        /// </summary>
        public override IFeatureCollection Features { get; }

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        internal IServiceScope ServiceScope { get; set; }

        public override CancellationToken ConnectionClosed { get; set; }

        public HttpConnectionContext(string connectionId, string connectionToken, ILogger logger)
        {
            ConnectionId = connectionId;
            ConnectionToken = connectionToken;
            _logger = logger;
            LastSeenUtc = DateTime.UtcNow;

            Features = new FeatureCollection();
            ActiveFormat = TransferFormat.Text;
            _connectionClosedTokenSource = new CancellationTokenSource();

            Features = new FeatureCollection();
            Features.Set<IConnectionUserFeature>(this);
            Features.Set<IConnectionHeartbeatFeature>(this);
            Features.Set<IConnectionInherentKeepAliveFeature>(this);
            Features.Set<IConnectionLifetimeFeature>(this);
            Features.Set<ITransferFormatFeature>(this);
            Features.Set<IHttpContextFeature>(this);

            SupportedFormats = TransferFormat.Binary | TransferFormat.Text;
        }

        public override IDuplexPipe Transport { get; set; }

        private PipeWriterStream _applicationStream;
        private IDuplexPipe _application;
        public IDuplexPipe Application
        {
            get => _application;
            set
            {
                if (value != null)
                {
                    _applicationStream = new PipeWriterStream(value.Output);
                }
                else
                {
                    _applicationStream = null;
                }
                _application = value;
            }
        }

        /// <summary>
        /// 运输类型
        /// </summary>
        public HttpTransportType TransportType { get; set; }

        /// <summary>
        /// 支持的传输格式
        /// </summary>
        public TransferFormat SupportedFormats { get; set; }

        /// <summary>
        /// http上下文
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// 当前用户
        /// </summary>
        public ClaimsPrincipal User { get; set; }

        /// <summary>
        /// 取消控制令牌
        /// </summary>
        public CancellationTokenSource Cancellation { get; set; }

        /// <summary>
        /// 传输消息格式
        /// </summary>
        public TransferFormat ActiveFormat { get; set; }

        public Task ApplicationTask { get; set; }

        public Task TransportTask { get; set; }

        /// <summary>
        /// 最后一次扫描时间 用于心跳检测
        /// </summary>
        public DateTime LastSeenUtc { get; set; }
        public DateTime? LastSeenUtcIfInactive
        {
            get
            {
                lock (_stateLock)
                {
                    return Status == HttpConnectionStatus.Inactive ? (DateTime?)LastSeenUtc : null;
                }
            }
        }

        private readonly object _sendingLock = new object();

        /// <summary>
        /// 发送控制令牌
        /// </summary>
        private CancellationTokenSource _sendCts;
        internal CancellationToken SendingToken { get; private set; }


        private bool _activeSend;
        private long _startedSendTime;
        internal void StartSendCancellation()
        {
            lock (_sendingLock)
            {
                if (_sendCts == null || _sendCts.IsCancellationRequested)
                {
                    _sendCts = new CancellationTokenSource();
                    SendingToken = _sendCts.Token;
                }
                _startedSendTime = DateTime.UtcNow.Ticks;
                _activeSend = true;
            }
        }

        internal void StopSendCancellation()
        {
            lock (_sendingLock)
            {
                _activeSend = false;
            }
        }

        public HttpConnectionStatus Status { get; set; } = HttpConnectionStatus.Inactive;
        private readonly object _stateLock = new object();
        internal bool TryActivatePersistentConnection(
            ConnectionDelegate connectionDelegate,
            IHttpTransport transport,
            ILogger dispatcherLogger)
         {
            lock (_stateLock)
            {
                if (Status == HttpConnectionStatus.Inactive)
                {
                    Status = HttpConnectionStatus.Active;

                    // 执行HubHandler中的代码
                    ApplicationTask = ExecuteApplication(connectionDelegate);

                    // 开启WebSocket运输
                    TransportTask = transport.ProcessRequestAsync(HttpContext, HttpContext.RequestAborted);

                    return true;
                }
                else
                {
                    FailActivationUnsynchronized(HttpContext, dispatcherLogger);

                    return false;
                }
            }
        }

        private async Task ExecuteApplication(ConnectionDelegate connectionDelegate)
        {
            Debug.Assert(TransportType != HttpTransportType.None, "Transport has not been initialized yet");

            // 跳转到线程池线程，这样阻止用户代码就不会阻止
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await connectionDelegate(this);
        }

        private void FailActivationUnsynchronized(HttpContext nonClonedContext, ILogger dispatcherLogger)
        {
            if (Status == HttpConnectionStatus.Active)
            {
                HttpConnectionDispatcher.Log.ConnectionAlreadyActive(dispatcherLogger, ConnectionId, HttpContext.TraceIdentifier);
                nonClonedContext.Response.StatusCode = StatusCodes.Status409Conflict;
                nonClonedContext.Response.ContentType = "text/plain";
            }
            else
            {
                Debug.Assert(Status == HttpConnectionStatus.Disposed);

                HttpConnectionDispatcher.Log.ConnectionDisposed(dispatcherLogger, ConnectionId);
                nonClonedContext.Response.StatusCode = StatusCodes.Status404NotFound;
                nonClonedContext.Response.ContentType = "text/plain";
            }
        }

        private readonly TaskCompletionSource _disposeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task DisposeAsync(bool closeGracefully = false)
        {
            Task disposeTask;

            try
            {
                lock (_stateLock)
                {
                    if (Status == HttpConnectionStatus.Disposed)
                    {
                        disposeTask = _disposeTcs.Task;
                    }
                    else
                    {
                        Status = HttpConnectionStatus.Disposed;

                        Log.DisposingConnection(_logger, ConnectionId);

                        var applicationTask = ApplicationTask ?? Task.CompletedTask;
                        var transportTask = TransportTask ?? Task.CompletedTask;

                        disposeTask = WaitOnTasks(applicationTask, transportTask, closeGracefully);
                    }
                }
            }
            finally
            {
                Cancellation?.Dispose();

                Cancellation = null;

                if (User != null && User.Identity is WindowsIdentity)
                {
                    foreach (var identity in User.Identities)
                    {
                        (identity as IDisposable)?.Dispose();
                    }
                }

                ServiceScope?.Dispose();
            }

            await disposeTask;
        }

        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);

        public bool HasInherentKeepAlive { get; set; }

        private CancellationTokenSource _connectionClosedTokenSource;
        private async Task WaitOnTasks(Task applicationTask, Task transportTask, bool closeGracefully)
        {
            try
            {
                // Closing gracefully means we're only going to close the finished sides of the pipe
                // If the application finishes, that means it's done with the transport pipe
                // If the transport finishes, that means it's done with the application pipe
                if (!closeGracefully)
                {
                    Application?.Output.CancelPendingFlush();

                    if (TransportType == HttpTransportType.WebSockets)
                    {
                        // The websocket transport will close the application output automatically when reading is canceled
                        Cancellation?.Cancel();
                    }
                    else
                    {
                        // Normally it isn't safe to try and acquire this lock because the Send can hold onto it for a long time if there is backpressure
                        // It is safe to wait for this lock now because the Send will be in one of 4 states
                        // 1. In the middle of a write which is in the middle of being canceled by the CancelPendingFlush above, when it throws
                        //    an OperationCanceledException it will complete the PipeWriter which will make any other Send waiting on the lock
                        //    throw an InvalidOperationException if they call Write
                        // 2. About to write and see that there is a pending cancel from the CancelPendingFlush, go to 1 to see what happens
                        // 3. Enters the Send and sees the Dispose state from DisposeAndRemoveAsync and releases the lock
                        // 4. No Send in progress
                        await WriteLock.WaitAsync();
                        try
                        {
                            // Complete the applications read loop
                            Application?.Output.Complete();
                        }
                        finally
                        {
                            WriteLock.Release();
                        }

                        Application?.Input.CancelPendingRead();
                    }
                }

                // Wait for either to finish
                var result = await Task.WhenAny(applicationTask, transportTask);

                // If the application is complete, complete the transport pipe (it's the pipe to the transport)
                if (result == applicationTask)
                {
                    Transport?.Output.Complete(applicationTask.Exception?.InnerException);
                    Transport?.Input.Complete();

                    try
                    {
                        Log.WaitingForTransport(_logger, TransportType);

                        // Transports are written by us and are well behaved, wait for them to drain
                        await transportTask;
                    }
                    finally
                    {
                        Log.TransportComplete(_logger, TransportType);

                        // Now complete the application
                        Application?.Output.Complete();
                        Application?.Input.Complete();

                        // Trigger ConnectionClosed
                        ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts).Cancel(), _connectionClosedTokenSource);
                    }
                }
                else
                {
                    // If the transport is complete, complete the application pipes
                    Application?.Output.Complete(transportTask.Exception?.InnerException);
                    Application?.Input.Complete();

                    // Trigger ConnectionClosed
                    ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts).Cancel(), _connectionClosedTokenSource);

                    try
                    {
                        // A poorly written application *could* in theory get stuck forever and it'll show up as a memory leak
                        Log.WaitingForApplication(_logger);

                        await applicationTask;
                    }
                    finally
                    {
                        Log.ApplicationComplete(_logger);

                        Transport?.Output.Complete();
                        Transport?.Input.Complete();
                    }
                }

                // Notify all waiters that we're done disposing
                _disposeTcs.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                _disposeTcs.TrySetCanceled();

                throw;
            }
            catch (Exception ex)
            {
                _disposeTcs.TrySetException(ex);

                throw;
            }
        }

        #region 心跳检测
        private readonly object _heartbeatLock = new object();
        private List<(Action<object> handler, object state)> _heartbeatHandlers;
        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }
                _heartbeatHandlers.Add((action, state));
            }
        }
        #endregion


        /// <summary>
        /// 挂起连接
        /// </summary>
        public override void Abort()
        {
            ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts).Cancel(), _connectionClosedTokenSource);

            HttpContext?.Abort();
        }

        private static long _tenSeconds = TimeSpan.FromSeconds(10).Ticks;
        /// <summary>
        /// 并且发送时间如果大于10秒，且心跳检测到超时 取消发送 
        /// </summary>
        /// <param name="currentTicks"></param>
        internal void TryCancelSend(long currentTicks)
        {
            lock (_sendingLock)
            {
                if (_activeSend)
                {
                    if (currentTicks - _startedSendTime > _tenSeconds)
                    {
                        _sendCts.Cancel();
                    }
                }
            }
        }

        public void TickHeartbeat()
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    return;
                }

                foreach (var (handler, state) in _heartbeatHandlers)
                {
                    handler(state);
                }
            }
        }

        #region 日志
        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _disposingConnection =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1, "DisposingConnection"), "Disposing connection {TransportConnectionId}.");

            private static readonly Action<ILogger, Exception> _waitingForApplication =
                LoggerMessage.Define(LogLevel.Trace, new EventId(2, "WaitingForApplication"), "Waiting for application to complete.");

            private static readonly Action<ILogger, Exception> _applicationComplete =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "ApplicationComplete"), "Application complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _waitingForTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(4, "WaitingForTransport"), "Waiting for {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportComplete =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(5, "TransportComplete"), "{TransportType} transport complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _shuttingDownTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(6, "ShuttingDownTransportAndApplication"), "Shutting down both the application and the {TransportType} transport.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _waitingForTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(7, "WaitingForTransportAndApplication"), "Waiting for both the application and {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportAndApplicationComplete =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(8, "TransportAndApplicationComplete"), "The application and {TransportType} transport are both complete.");

            public static void DisposingConnection(ILogger logger, string connectionId)
            {
                if (logger == null)
                {
                    return;
                }

                _disposingConnection(logger, connectionId, null);
            }

            public static void WaitingForApplication(ILogger logger)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForApplication(logger, null);
            }

            public static void ApplicationComplete(ILogger logger)
            {
                if (logger == null)
                {
                    return;
                }

                _applicationComplete(logger, null);
            }

            public static void WaitingForTransport(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForTransport(logger, transportType, null);
            }

            public static void TransportComplete(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _transportComplete(logger, transportType, null);
            }
            public static void ShuttingDownTransportAndApplication(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _shuttingDownTransportAndApplication(logger, transportType, null);
            }

            public static void WaitingForTransportAndApplication(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForTransportAndApplication(logger, transportType, null);
            }

            public static void TransportAndApplicationComplete(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _transportAndApplicationComplete(logger, transportType, null);
            }
        } 
        #endregion
    }
}
