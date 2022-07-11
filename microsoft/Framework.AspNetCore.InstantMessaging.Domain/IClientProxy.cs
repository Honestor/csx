using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IClientProxy
    {
        Task SendCoreAsync(string method, object?[]? args, CancellationToken cancellationToken = default);
    }
}
