using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubCallerClients : IHubCallerClients
    {
        private readonly string _connectionId;
        private readonly IHubClients _hubClients;
        private readonly string[] _currentConnectionId;

        public HubCallerClients(IHubClients hubClients, string connectionId)
        {
            _connectionId = connectionId;
            _hubClients = hubClients;
            _currentConnectionId = new[] { _connectionId };
        }

        public IClientProxy All => _hubClients.All;

        public IClientProxy Group(string groupName)
        {
            return _hubClients.Group(groupName);
        }
    }
}
