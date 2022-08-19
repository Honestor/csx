

using Framework.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub连接上下文
    /// </summary>
    public partial class HubConnectionContext
    {
        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly long _keepAliveInterval;
        private readonly long _clientTimeoutInterval;
        private long? _maxMessageSize;
        private readonly ISystemClock _systemClock;

        /// <summary>
        /// 连接挂起触发Token
        /// </summary>
        public virtual CancellationToken ConnectionAborted { get; }
        public HubConnectionContext(ConnectionContext connectionContext, HubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
        {
            _keepAliveInterval = contextOptions.KeepAliveInterval.Ticks;
            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval.Ticks;
            _maxMessageSize = contextOptions.MaximumReceiveMessageSize;
            _connectionContext = connectionContext;
            _logger = loggerFactory.CreateLogger<HubConnectionContext>();
            _systemClock = contextOptions.SystemClock ?? new SystemClock();
            _lastSendTimeStamp = _systemClock.UtcNowTicks;

            ConnectionAborted = _connectionAbortedTokenSource.Token;
            _closedRegistration = connectionContext.ConnectionClosed.Register((state) => ((HubConnectionContext)state!).Abort(), this);

            HubCallerContext = new DefaultHubCallerContext(this);
            var maxInvokeLimit = contextOptions.MaximumParallelInvocations;
            if (maxInvokeLimit != 1)
            {
                ActiveInvocationLimit = new SemaphoreSlim(maxInvokeLimit, maxInvokeLimit);
            }
        }

        internal SemaphoreSlim? ActiveInvocationLimit { get; }

        public virtual IHubProtocol Protocol { get; set; } = default!;

        internal PipeReader Input => _connectionContext.Transport.Input;

        private static readonly Action<object?> _cancelReader = state => ((PipeReader)state!).CancelPendingRead();

        /// <summary>
        /// Feature集合
        /// </summary>
        public virtual IFeatureCollection Features => _connectionContext.Features;

        private ReadOnlyMemory<byte> _cachedPingMessage;

        /// <summary>
        /// 当前用户
        /// </summary>
        public string? UserIdentifier { get; set; }

        /// <summary>
        /// 当前连接是否被挂起了
        /// </summary>
        private volatile bool _connectionAborted;

        /// <summary>
        /// 关闭异常
        /// </summary>
        internal Exception? CloseException { get; private set; }

        /// <summary>
        /// 挂起回调
        /// </summary>
        private static readonly WaitCallback _abortedCallback = AbortConnection;

        /// <summary>
        /// 连接Id
        /// </summary>
        public virtual string ConnectionId => _connectionContext.ConnectionId;

        private readonly TaskCompletionSource _abortCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 客户端Caller
        /// </summary>
        internal HubCallerContext HubCallerContext { get; }

        /// <summary>
        /// 是否允许重连
        /// </summary>
        private volatile bool _allowReconnect = true;
        internal bool AllowReconnect => _allowReconnect;

        public virtual ClaimsPrincipal? User
        {
            get {
                var f = Features.Get<IConnectionUserFeature>();
                return f.User;
            }
        }

        /// <summary>
        /// 握手
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <param name="supportedProtocols">支持的协议</param>
        /// <param name="protocolResolver">协议解析</param>
        /// <param name="userIdProvider">用户标识</param>
        /// <param name="enableDetailedErrors">是否向客户端发送详细的错误消息</param>
        /// <returns></returns>
        internal async Task<bool> HandshakeAsync(TimeSpan timeout, IReadOnlyList<string>? supportedProtocols, IHubProtocolResolver protocolResolver,
            IUserIdProvider userIdProvider, bool enableDetailedErrors)
        {
            try
            {
                var input = Input;
                using (var cts = new CancellationTokenSource())
                using (var registration = cts.Token.UnsafeRegister(_cancelReader, input))
                {
                    if (!Debugger.IsAttached)
                    {
                        cts.CancelAfter(timeout);
                    }

                    while (true)
                    {
                        var result = await input.ReadAsync();
                        var buffer = result.Buffer;
                        var consumed = buffer.Start;
                        var examined = buffer.End;

                        try
                        {
                            if (result.IsCanceled)
                            {
                                Log.HandshakeCanceled(_logger);
                                await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                                return false;
                            }

                            if (!buffer.IsEmpty)
                            {
                                var segment = buffer;
                                var overLength = false;

                                //传入消息大于最大值,截取
                                if (_maxMessageSize != null && buffer.Length > _maxMessageSize.Value)
                                {
                                    segment = segment.Slice(segment.Start, _maxMessageSize.Value);
                                    overLength = true;
                                }

                                if (HandshakeProtocol.TryParseRequestMessage(ref segment, out var handshakeRequestMessage))
                                {
                                    consumed = segment.Start;
                                    examined = consumed;

                                    Protocol = protocolResolver.GetProtocol(handshakeRequestMessage.Protocol, supportedProtocols)!;
                                    if (Protocol == null)
                                    {
                                        Log.HandshakeFailed(_logger, null);

                                        await WriteHandshakeResponseAsync(new HandshakeResponseMessage($"The protocol '{handshakeRequestMessage.Protocol}' is not supported."));
                                        return false;
                                    }

                                    if (!Protocol.IsVersionSupported(handshakeRequestMessage.Version))
                                    {
                                        Log.ProtocolVersionFailed(_logger, handshakeRequestMessage.Protocol, handshakeRequestMessage.Version);
                                        await WriteHandshakeResponseAsync(new HandshakeResponseMessage(
                                            $"The server does not support version {handshakeRequestMessage.Version} of the '{handshakeRequestMessage.Protocol}' protocol."));
                                        return false;
                                    }

                                    // If there's a transfer format feature, we need to check if we're compatible and set the active format.
                                    // If there isn't a feature, it means that the transport supports binary data and doesn't need us to tell them
                                    // what format we're writing.
                                    var transferFormatFeature = Features.Get<ITransferFormatFeature>();
                                    if (transferFormatFeature != null)
                                    {
                                        if ((transferFormatFeature.SupportedFormats & Protocol.TransferFormat) == 0)
                                        {
                                            Log.HandshakeFailed(_logger, null);
                                            await WriteHandshakeResponseAsync(new HandshakeResponseMessage($"Cannot use the '{Protocol.Name}' protocol on the current transport. The transport does not support '{Protocol.TransferFormat}' transfer format."));
                                            return false;
                                        }

                                        transferFormatFeature.ActiveFormat = Protocol.TransferFormat;
                                    }

                                    _cachedPingMessage = Protocol.GetMessageBytes(PingMessage.Instance);

                                    UserIdentifier = userIdProvider.GetUserId(this);

                                    if (Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
                                    {
                                        // Only register KeepAlive after protocol handshake otherwise KeepAliveTick could try to write without having a ProtocolReaderWriter
                                        Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).KeepAliveTick(), this);
                                    }

                                    Log.HandshakeComplete(_logger, Protocol.Name);

                                    await WriteHandshakeResponseAsync(HandshakeResponseMessage.Empty);
                                    return true;
                                }
                                else if (overLength)
                                {
                                    Log.HandshakeSizeLimitExceeded(_logger, _maxMessageSize!.Value);
                                    await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                                    return false;
                                }
                            }

                            if (result.IsCompleted)
                            {
                                // connection was closed before we ever received a response
                                // can't send a handshake response because there is no longer a connection
                                Log.HandshakeFailed(_logger, null);
                                return false;
                            }
                        }
                        finally
                        {
                            input.AdvanceTo(consumed, examined);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.HandshakeCanceled(_logger);
                await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                return false;
            }
            catch (Exception ex)
            {
                Log.HandshakeFailed(_logger, ex);
                var errorMessage = ErrorMessageHelper.BuildErrorMessage("An unexpected error occurred during connection handshake.", ex, enableDetailedErrors);
                await WriteHandshakeResponseAsync(new HandshakeResponseMessage(errorMessage));
                return false;
            }
        }


        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        /// <summary>
        /// 写入握手响应
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task WriteHandshakeResponseAsync(HandshakeResponseMessage message)
        {
            await _writeLock.WaitAsync();

            try
            {
                if (message.Error == null)
                {
                    _connectionContext.Transport.Output.Write(HandshakeProtocol.GetSuccessfulHandshake(Protocol));
                }
                else
                {
                    HandshakeProtocol.WriteResponseMessage(message, _connectionContext.Transport.Output); //传入连接得输出管道写入数据
                }

                await _connectionContext.Transport.Output.FlushAsync(); 
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private long _lastSendTimeStamp;
        private void KeepAliveTick()
        {
            var currentTime = _systemClock.UtcNowTicks;

            //如果当前时间减去上一次发送心跳的时间大于配置的心跳检测时间间隔,说明需要发送心跳了
            if (currentTime - Volatile.Read(ref _lastSendTimeStamp) > _keepAliveInterval)
            {
                //在整个keep alive持续时间内未发送消息，请发送ping
                //如果传输通道已满，这将失败，但这没关系，因为
                //在传输已满时添加Ping消息是不必要的，因为
                //传输仍在发送帧的过程中。
                _ = TryWritePingAsync();

                // We only update the timestamp here, because updating on each sent message is bad for performance
                // There can be a lot of sent messages per 15 seconds
                Volatile.Write(ref _lastSendTimeStamp, currentTime);
            }
        }

        private ValueTask TryWritePingAsync()
        {   
            //不等待锁,如果返回false,说明客户端写入了消息,ping不是必须的
            if (!_writeLock.Wait(0))
            {
                return default;
            }

            return new ValueTask(TryWritePingSlowAsync());
        }

        /// <summary>
        /// 写入ping消息
        /// </summary>
        /// <returns></returns>
        private async Task TryWritePingSlowAsync()
        {
            try
            {
                //挂起直接返回了
                if (_connectionAborted)
                {
                    return;
                }

                await _connectionContext.Transport.Output.WriteAsync(_cachedPingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        /// <summary>
        /// 挂起允许重连
        /// </summary>
        private void AbortAllowReconnect()
        {
            //设置连接挂起
            _connectionAborted = true;

            //取消任何当前写入或即将发生的写入必须在锁外执行此操作，否则如果写操作检测到压力，可能会挂起
            _connectionContext.Transport.Output.CancelPendingFlush();

            //如果连接挂起则什么都不做
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            //取消读取
            Input.CancelPendingRead();

            // We fire and forget since this can trigger user code to run
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
        }

        /// <summary>
        /// 挂起连接
        /// </summary>
        /// <param name="state"></param>
        private static void AbortConnection(object? state)
        {
            var connection = (HubConnectionContext)state!;

            try
            {
                connection._connectionAbortedTokenSource.Cancel();//触发挂起令牌
            }
            catch (Exception ex)
            {
                Log.AbortFailed(connection._logger, ex);
            }
            finally
            {
                _ = InnerAbortConnection(connection);
            }

            static async Task InnerAbortConnection(HubConnectionContext connection)
            {
                // 我们锁定以确保在触发管道完成之前完成所有写入
                await connection._writeLock.WaitAsync();
                try
                {
                    // 传达已经完成触发中止回调的事实 HubOnDisconnectedAsync正在等待完成管道
                    connection._abortCompletedTcs.TrySetResult();
                }
                finally
                {
                    connection._writeLock.Release();
                }
            }
        }

        public virtual ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            return WriteAsync(message, ignoreAbort: false, cancellationToken);
        }

        internal ValueTask WriteAsync(HubMessage message, bool ignoreAbort, CancellationToken cancellationToken = default)
        {
            // 如果锁被占用,等待锁
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message, ignoreAbort, cancellationToken));
            }

            //如果设置了挂起是重要的且当前连接处于挂起状态则什么都不做
            if (_connectionAborted && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // 写操作没有完成，所以请等待完成
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            //释放WriteAsync时获取的锁
            _writeLock.Release();
            return default;
        }

        private async Task WriteSlowAsync(HubMessage message, bool ignoreAbort, CancellationToken cancellationToken)
        {
            //等待锁,直到它可用
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                if (_connectionAborted && !ignoreAbort)
                {
                    return;
                }

                await WriteCore(message, cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private ValueTask<FlushResult> WriteCore(HubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Protocol.WriteMessage(message, _connectionContext.Transport.Output);
                return _connectionContext.Transport.Output.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private async Task CompleteWriteAsync(ValueTask<FlushResult> task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private readonly object _receiveMessageTimeoutLock = new object();
        private bool _receivedMessageTimeoutEnabled = false;
        private long _receivedMessageTimestamp;
        /// <summary>
        /// 开启收到的消息处理超时功能
        /// </summary>
        internal void BeginClientTimeout()
        {
            lock (_receiveMessageTimeoutLock)
            {
                _receivedMessageTimeoutEnabled = true;
                _receivedMessageTimestamp = _systemClock.UtcNowTicks;
            }
        }

        private long _receivedMessageElapsedTicks = 0;
        /// <summary>
        /// 收到消息，所以请停止计时器并重置它,消息处理后恢复
        /// </summary>
        internal void StopClientTimeout()
        {
            lock (_receiveMessageTimeoutLock)
            {
                _receivedMessageElapsedTicks = 0;
                _receivedMessageTimestamp = 0;
                _receivedMessageTimeoutEnabled = false;
            }
        }

        /// <summary>
        /// 挂起
        /// </summary>
        /// <returns></returns>
        internal Task AbortAsync()
        {
            AbortAllowReconnect();

            // 获取锁以确保所有写入都已完成
            if (!_writeLock.Wait(0))
            {
                return AbortAsyncSlow();
            }
            _writeLock.Release();
            return _abortCompletedTcs.Task;
        }

        /// <summary>
        /// 等待所有写入完成,挂起
        /// </summary>
        /// <returns></returns>
        private async Task AbortAsyncSlow()
        {
            await _writeLock.WaitAsync();
            _writeLock.Release();
            await _abortCompletedTcs.Task;
        }

        private readonly CancellationTokenRegistration _closedRegistration;
        internal void Cleanup()
        {
            _closedRegistration.Dispose();
        }

        public virtual void Abort()
        {
            _allowReconnect = false;
            AbortAllowReconnect();
        }

        private bool _clientTimeoutActive;
        internal void StartClientTimeout()
        {
            if (_clientTimeoutActive)
            {
                return;
            }
            _clientTimeoutActive = true;
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).CheckClientTimeout(), this);
        }

        /// <summary>
        /// 检查客户端有没有超时
        /// </summary>
        private void CheckClientTimeout()
        {
            if (Debugger.IsAttached)
            {
                return;
            }

            lock (_receiveMessageTimeoutLock)
            {
                if (_receivedMessageTimeoutEnabled)
                {
                    _receivedMessageElapsedTicks = _systemClock.UtcNowTicks - _receivedMessageTimestamp;

                    if (_receivedMessageElapsedTicks >= _clientTimeoutInterval)
                    {
                        Log.ClientTimeout(_logger, TimeSpan.FromTicks(_clientTimeoutInterval));
                        AbortAllowReconnect();
                    }
                }
            }
        }

        /// <summary>
        /// 写入消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual ValueTask WriteAsync(SerializedHubMessage message, CancellationToken cancellationToken = default)
        {
            //如果写入锁被占用了
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message, cancellationToken));
            }

            if (_connectionAborted)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            // Otherwise, release the lock acquired when entering WriteAsync
            _writeLock.Release();

            return default;
        }

        private async Task WriteSlowAsync(SerializedHubMessage message, CancellationToken cancellationToken)
        {
            // 等待写入锁的释放,直到可用
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                //如果此时连接挂起,不写入
                if (_connectionAborted)
                {
                    return;
                }

                await WriteCore(message, cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private ValueTask<FlushResult> WriteCore(SerializedHubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = message.GetSerializedMessage(Protocol);

                return _connectionContext.Transport.Output.WriteAsync(buffer, cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }
    }
}
