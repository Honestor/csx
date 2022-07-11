namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// 是否开启心跳检测Feature 长轮询不需要
    /// </summary>
    public interface IConnectionInherentKeepAliveFeature
    {
        bool HasInherentKeepAlive { get; }
    }
}