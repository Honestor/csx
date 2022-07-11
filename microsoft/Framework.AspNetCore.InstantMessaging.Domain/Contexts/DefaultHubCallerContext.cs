using Microsoft.AspNetCore.Http.Features;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class DefaultHubCallerContext : HubCallerContext
    {
        private readonly HubConnectionContext _connection;

        public DefaultHubCallerContext(HubConnectionContext connection)
        {
            _connection = connection;
        }

        public override string ConnectionId => _connection.ConnectionId;

        public override IFeatureCollection Features => _connection.Features;
    }
}
