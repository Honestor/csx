using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.IdentityServer4.Domain
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
            var request = context.Request;

            context.SetIdentityServerOrigin("http://localhost:45345/identityserver/");

            await _next(context);
        }
    }
}
