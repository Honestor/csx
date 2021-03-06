// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Ms.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class TokenResponseReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Creates a <see cref="TokenResponseReceivedContext"/>
        /// </summary>
        public TokenResponseReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal user, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = user;

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        public OpenIdConnectMessage ProtocolMessage { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/> that contains the tokens received after redeeming the code at the token endpoint.
        /// </summary>
        public OpenIdConnectMessage TokenEndpointResponse { get; set; }
    }
}
