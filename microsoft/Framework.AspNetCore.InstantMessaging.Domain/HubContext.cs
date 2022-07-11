namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubContext<THub> : IHubContext<THub> where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubClients _clients;

        public HubContext(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            _clients = new HubClients<THub>(_lifetimeManager);
            Groups = new GroupManager<THub>(lifetimeManager);
        }

        public IHubClients Clients => _clients;

        public virtual IGroupManager Groups { get; }
    }
}
