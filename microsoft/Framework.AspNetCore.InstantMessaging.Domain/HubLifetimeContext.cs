// Copyright (c) .NET Foundation. All rights reserved.
using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public sealed class HubLifetimeContext
    {
        public Hub Hub { get; }

        public IServiceProvider ServiceProvider { get; }

        public HubCallerContext Context { get; }

        public HubLifetimeContext(HubCallerContext context, IServiceProvider serviceProvider, Hub hub)
        {
            Hub = hub;
            ServiceProvider = serviceProvider;
            Context = context;
        }
    }
}
