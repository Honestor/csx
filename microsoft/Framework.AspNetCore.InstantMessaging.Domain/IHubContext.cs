namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IHubContext<THub> where THub : Hub
    {
        /// <summary>
        /// ¿Í»§¶Ë
        /// </summary>
        IHubClients Clients { get; }

        /// <summary>
        /// ×é
        /// </summary>
        IGroupManager Groups { get; }
    }
}
