namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// 提供对客户端连接（包括发送当前调用的连接）的访问的抽象。
    /// </summary>
    /// <typeparam name="T">The client caller type.</typeparam>
    public interface IHubCallerClients<T> : IHubClients<T>
    {
        
    }
}
