// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Class to configure the <see cref="HubOptions"/> for a specific <typeparamref name="THub"/>.
    /// </summary>
    /// <typeparam name="THub">The <see cref="Hub"/> type to configure.</typeparam>
    public class HubOptionsSetup<THub> : IConfigureOptions<HubOptions<THub>> where THub : Hub
    {
        private readonly HubOptions _hubOptions;

        public HubOptionsSetup(IOptions<HubOptions> options)
        {
            _hubOptions = options.Value;
        }

        /// <summary>
        /// Configures the default values of the <see cref="HubOptions"/>.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        public void Configure(HubOptions<THub> options)
        {
            options.SupportedProtocols = new List<string>(_hubOptions.SupportedProtocols ?? Array.Empty<string>());
            options.KeepAliveInterval = _hubOptions.KeepAliveInterval;
            options.HandshakeTimeout = _hubOptions.HandshakeTimeout;
            options.ClientTimeoutInterval = _hubOptions.ClientTimeoutInterval;
            options.MaximumReceiveMessageSize = _hubOptions.MaximumReceiveMessageSize;
            options.UserHasSetValues = true;
            options.EnableDetailedErrors = _hubOptions.EnableDetailedErrors;
        }
    }
}
