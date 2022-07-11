using System;
using Framework.AspNetCore.InstantMessaging.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Framework.AspNetCore.InstantMessaging.Application
{
    public static class GetHttpContextExtensions
    {
        public static HttpContext GetHttpContext(this HubCallerContext connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }

        public static HttpContext GetHttpContext(this HubConnectionContext connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }
    }
}
