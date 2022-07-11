using System;

namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// �������Feature
    /// </summary>
    public interface IConnectionHeartbeatFeature
    {
        void OnHeartbeat(Action<object> action, object state);
    }
}