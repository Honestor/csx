using System;
using System.IO.Pipelines;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public abstract class ConnectionContext: BaseConnectionContext, IAsyncDisposable
    {
        /// <summary>
        /// ��������˫����
        /// </summary>
        public abstract IDuplexPipe Transport { get; set; }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            Features.Get<IConnectionLifetimeFeature>()?.Abort();
        }
    }
}
