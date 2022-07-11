using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub协议Resolver
    /// </summary>
    public interface IHubProtocolResolver
    {
        /// <summary>
        /// 所有可用的协议协议
        /// </summary>
        IReadOnlyList<IHubProtocol> AllProtocols { get; }

        /// <summary>
        /// 获取协议
        /// </summary>
        /// <param name="protocolName"></param>
        /// <param name="supportedProtocols"></param>
        /// <returns></returns>
        IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols);
    }
}
