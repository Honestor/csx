

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
    /// Hub����������
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
        /// ���ӹ��𴥷�Token
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
        /// Feature����
        /// </summary>
        public virtual IFeatureCollection Features => _connectionContext.Features;

        private ReadOnlyMemory<byte> _cachedPingMessage;

        /// <summary>
        /// ��ǰ�û�
        /// </summary>
        public string? UserIdentifier { get; set; }

        /// <summary>
        /// ��ǰ�����Ƿ񱻹�����
        /// </summary>
        private volatile bool _connectionAborted;

        /// <summary>
        /// �ر��쳣
        /// </summary>
        internal Exception? CloseException { get; private set; }

        /// <summary>
        /// ����ص�
        /// </summary>
        private static readonly WaitCallback _abortedCallback = AbortConnection;

        /// <summary>
        /// ����Id
        /// </summary>
        public virtual string ConnectionId => _connectionContext.ConnectionId;

        private readonly TaskCompletionSource _abortCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// �ͻ���Caller
        /// </summary>
        internal HubCallerContext HubCallerContext { get; }

        /// <summary>
        /// �Ƿ���������
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
        /// ����
        /// </summary>
        /// <param name="timeout">��ʱʱ��</param>
        /// <param name="supportedProtocols">֧�ֵ�Э��</param>
        /// <param name="protocolResolver">Э�����</param>
        /// <param name="userIdProvider">�û���ʶ</param>
        /// <param name="enableDetailedErrors">�Ƿ���ͻ��˷�����ϸ�Ĵ�����Ϣ</param>
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

                                //������Ϣ�������ֵ,��ȡ
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
        /// д��������Ӧ
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
                    HandshakeProtocol.WriteResponseMessage(message, _connectionContext.Transport.Output); //�������ӵ�����ܵ�д������
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

            //�����ǰʱ���ȥ��һ�η���������ʱ��������õ��������ʱ����,˵����Ҫ����������
            if (currentTime - Volatile.Read(ref _lastSendTimeStamp) > _keepAliveInterval)
            {
                //������keep alive����ʱ����δ������Ϣ���뷢��ping
                //�������ͨ���������⽫ʧ�ܣ�����û��ϵ����Ϊ
                //�ڴ�������ʱ���Ping��Ϣ�ǲ���Ҫ�ģ���Ϊ
                //�������ڷ���֡�Ĺ����С�
                _ = TryWritePingAsync();

                // We only update the timestamp here, because updating on each sent message is bad for performance
                // There can be a lot of sent messages per 15 seconds
                Volatile.Write(ref _lastSendTimeStamp, currentTime);
            }
        }

        private ValueTask TryWritePingAsync()
        {   
            //���ȴ���,�������false,˵���ͻ���д������Ϣ,ping���Ǳ����
            if (!_writeLock.Wait(0))
            {
                return default;
            }

            return new ValueTask(TryWritePingSlowAsync());
        }

        /// <summary>
        /// д��ping��Ϣ
        /// </summary>
        /// <returns></returns>
        private async Task TryWritePingSlowAsync()
        {
            try
            {
                //����ֱ�ӷ�����
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
        /// ������������
        /// </summary>
        private void AbortAllowReconnect()
        {
            //�������ӹ���
            _connectionAborted = true;

            //ȡ���κε�ǰд��򼴽�������д�����������ִ�д˲������������д������⵽ѹ�������ܻ����
            _connectionContext.Transport.Output.CancelPendingFlush();

            //������ӹ�����ʲô������
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            //ȡ����ȡ
            Input.CancelPendingRead();

            // We fire and forget since this can trigger user code to run
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="state"></param>
        private static void AbortConnection(object? state)
        {
            var connection = (HubConnectionContext)state!;

            try
            {
                connection._connectionAbortedTokenSource.Cancel();//������������
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
                // ����������ȷ���ڴ����ܵ����֮ǰ�������д��
                await connection._writeLock.WaitAsync();
                try
                {
                    // �����Ѿ���ɴ�����ֹ�ص�����ʵ HubOnDisconnectedAsync���ڵȴ���ɹܵ�
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
            // �������ռ��,�ȴ���
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message, ignoreAbort, cancellationToken));
            }

            //��������˹�������Ҫ���ҵ�ǰ���Ӵ��ڹ���״̬��ʲô������
            if (_connectionAborted && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // д����û����ɣ�������ȴ����
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            //�ͷ�WriteAsyncʱ��ȡ����
            _writeLock.Release();
            return default;
        }

        private async Task WriteSlowAsync(HubMessage message, bool ignoreAbort, CancellationToken cancellationToken)
        {
            //�ȴ���,ֱ��������
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
        /// �����յ�����Ϣ����ʱ����
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
        /// �յ���Ϣ��������ֹͣ��ʱ����������,��Ϣ�����ָ�
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
        /// ����
        /// </summary>
        /// <returns></returns>
        internal Task AbortAsync()
        {
            AbortAllowReconnect();

            // ��ȡ����ȷ������д�붼�����
            if (!_writeLock.Wait(0))
            {
                return AbortAsyncSlow();
            }
            _writeLock.Release();
            return _abortCompletedTcs.Task;
        }

        /// <summary>
        /// �ȴ�����д�����,����
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
        /// ���ͻ�����û�г�ʱ
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
        /// д����Ϣ
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual ValueTask WriteAsync(SerializedHubMessage message, CancellationToken cancellationToken = default)
        {
            //���д������ռ����
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
            // �ȴ�д�������ͷ�,ֱ������
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                //�����ʱ���ӹ���,��д��
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
