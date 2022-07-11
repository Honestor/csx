using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// HubЭ��Resolver
    /// </summary>
    public interface IHubProtocolResolver
    {
        /// <summary>
        /// ���п��õ�Э��Э��
        /// </summary>
        IReadOnlyList<IHubProtocol> AllProtocols { get; }

        /// <summary>
        /// ��ȡЭ��
        /// </summary>
        /// <param name="protocolName"></param>
        /// <param name="supportedProtocols"></param>
        /// <returns></returns>
        IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols);
    }
}
