using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Ms.Oidc.Tests
{
    /// <summary>
    /// 网关中间件
    /// </summary>
    public class GetWayMiddleware
    {
        private readonly RequestDelegate _next;

        public GetWayMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            //if (context == null) throw new ArgumentNullException(nameof(context));
            //if (value == null) throw new ArgumentNullException(nameof(value));

            //var split = value.Split(new[] { "://" }, StringSplitOptions.RemoveEmptyEntries);

            //var request = context.Request;
            //request.Scheme = split.First();
            //request.Host = new HostString(split.Last());
        }
    }
}
