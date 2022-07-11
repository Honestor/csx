using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IHubClients : IHubClients<IClientProxy> { }

    /// <summary>
    /// 提供对客户端连接的访问的抽象
    /// </summary>
    public interface IHubClients<T>
    {
        T All { get; }

        T Group(string groupName);
    }
}

