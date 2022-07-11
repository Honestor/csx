using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class GroupManager<THub> : IGroupManager where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupManager(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.AddToGroupAsync(connectionId, groupName, cancellationToken);
        }

    }
}
