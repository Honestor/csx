using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class DefaultHubActivator<THub> : IHubActivator<THub> where THub: Hub
    {
        private static readonly Lazy<ObjectFactory> _objectFactory = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(typeof(THub), Type.EmptyTypes));
        private readonly IServiceProvider _serviceProvider;
        private bool? _created;

        public DefaultHubActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual THub Create()
        {
            Debug.Assert(!_created.HasValue, "hub activators must not be reused.");

            _created = false;
            var hub = _serviceProvider.GetService<THub>();
            if (hub == null)
            {
                hub = (THub)_objectFactory.Value(_serviceProvider, Array.Empty<object>());
                _created = true;
            }

            return hub;
        }

        public virtual void Release(THub hub)
        {
            if (hub == null)
            {
                throw new ArgumentNullException(nameof(hub));
            }

            Debug.Assert(_created.HasValue, "hubs must be released with the hub activator they were created");

            if (_created.Value)
            {
                hub.Dispose();
            }
        }
    }
}
