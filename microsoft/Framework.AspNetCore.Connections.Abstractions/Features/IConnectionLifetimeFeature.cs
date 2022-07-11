using System.Threading;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public interface IConnectionLifetimeFeature
    {
        CancellationToken ConnectionClosed { get; set; }

        void Abort();
    }
}
