using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Framework.AspNetCore.Connections
{
    internal interface IHttpTransport
    {
        Task ProcessRequestAsync(HttpContext context, CancellationToken token);
    }
}
