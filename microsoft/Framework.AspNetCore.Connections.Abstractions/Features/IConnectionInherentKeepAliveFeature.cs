namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// �Ƿ����������Feature ����ѯ����Ҫ
    /// </summary>
    public interface IConnectionInherentKeepAliveFeature
    {
        bool HasInherentKeepAlive { get; }
    }
}