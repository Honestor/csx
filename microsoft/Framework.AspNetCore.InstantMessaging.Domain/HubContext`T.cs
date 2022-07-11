namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubContext<THub, T> : IHubContext<THub, T>
        where THub : Hub<T>
        where T : class
    {
        public IHubClients<T> Clients => throw new System.NotImplementedException();

        public IGroupManager Groups => throw new System.NotImplementedException();
    }
}
