using System;
using System.IO.Pipelines;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public abstract class ConnectionContext: BaseConnectionContext, IAsyncDisposable
    {
        /// <summary>
        /// 传入连接双工管
        /// </summary>
        public abstract IDuplexPipe Transport { get; set; }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            Features.Get<IConnectionLifetimeFeature>()?.Abort();
        }
    }
}
