using Microsoft.AspNetCore.Http.Features;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public abstract class HubCallerContext
    {
        public abstract IFeatureCollection Features { get; }

        public abstract string ConnectionId { get; }
    }
}
