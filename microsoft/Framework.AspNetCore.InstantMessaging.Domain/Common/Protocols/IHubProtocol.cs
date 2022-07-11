using Framework.AspNetCore.Connections.Abstractions;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Framework.AspNetCore.InstantMessaging.Domain
{

    public interface IHubProtocol
    {
        /// <summary>
        /// Э������
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Э��汾
        /// </summary>
        int Version { get; }

        /// <summary>
        /// �������ݸ�ʽ
        /// </summary>
        TransferFormat TransferFormat { get; }

        /// <summary>
        /// �汾�Ƿ�֧��
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        bool IsVersionSupported(int version);

        /// <summary>
        /// ��ȡ��Ϣ���ֽ�
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> GetMessageBytes(HubMessage message);

        /// <summary>
        /// д����Ϣ
        /// </summary>
        /// <param name="message"></param>
        /// <param name="output"></param>
        void WriteMessage(HubMessage message, IBufferWriter<byte> output);

        /// <summary>
        /// ת����Ϣ
        /// </summary>
        /// <param name="input"></param>
        /// <param name="binder"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage message);
    }
}
