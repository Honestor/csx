using System;

namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// ĞÄÌø¼ì²âFeature
    /// </summary>
    public interface IConnectionHeartbeatFeature
    {
        void OnHeartbeat(Action<object> action, object state);
    }
}