using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubClients<THub> : IHubClients where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy<THub>(_lifetimeManager);
        }

        public IClientProxy All { get; }

        public IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }
    }
}
