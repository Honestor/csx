using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class AllClientProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public AllClientProxy(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task SendCoreAsync(string method, object?[]? args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendAllAsync(method, args, cancellationToken);
        }
    }

    internal class GroupProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly string _groupName;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupProxy(HubLifetimeManager<THub> lifetimeManager, string groupName)
        {
            _lifetimeManager = lifetimeManager;
            _groupName = groupName;
        }

        public Task SendCoreAsync(string method, object?[]? args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendGroupAsync(_groupName, method, args, cancellationToken);
        }
    }
}
