// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IdentityServer4
{
    /// <summary>
    /// Model properties of an IdentityServer user
    /// </summary>
    public class IdentityServerUser
    {
        /// <summary>
        /// Subject ID (mandatory)
        /// </summary>
        public string SubjectId { get; }

        /// <summary>
        /// Display name (optional)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Identity provider (optional)
        /// </summary>
        public string IdentityProvider { get; set; }

        /// <summary>
        /// Authentication methods
        /// </summary>
        public ICollection<string> AuthenticationMethods { get; set; } = new HashSet<string>();

        /// <summary>
        /// Authentication time
        /// </summary>
        public DateTime? AuthenticationTime { get; set; }

        /// <summary>
        /// Additional claims
        /// </summary>
        public ICollection<Claim> AdditionalClaims { get; set; } = new HashSet<Claim>(new ClaimComparer());

        /// <summary>
        /// Initializes a user identity
        /// </summary>
        /// <param name="subjectId">The subject ID</param>
        public IdentityServerUser(string subjectId)
        {
            if (subjectId.IsMissing()) throw new ArgumentException("SubjectId is mandatory", nameof(subjectId));

            SubjectId = subjectId;
        }

        /// <summary>
        /// Creates an IdentityServer claims principal
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ClaimsPrincipal CreatePrincipal()
        {
            //向claims写入用户id
            if (SubjectId.IsMissing()) throw new ArgumentException("SubjectId is mandatory", nameof(SubjectId));
            var claims = new List<Claim> { new Claim(JwtClaimTypes.Subject, SubjectId) };

            //向claims写入用户名
            if (DisplayName.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.Name, DisplayName));
            }

            //向claims写入身份提供商,如果id4服务和identity部署在一台服务器上,那么默认时local
            if (IdentityProvider.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.IdentityProvider, IdentityProvider));
            }

            //向claims写入认证通过的时间
            if (AuthenticationTime.HasValue)
            {
                claims.Add(new Claim(JwtClaimTypes.AuthenticationTime, new DateTimeOffset(AuthenticationTime.Value).ToUnixTimeSeconds().ToString()));
            }

            //向claims写入认证方式
            if (AuthenticationMethods.Any())
            {
                foreach (var amr in AuthenticationMethods)
                {
                    claims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, amr));
                }
            }

            claims.AddRange(AdditionalClaims);

            var id = new ClaimsIdentity(claims.Distinct(new ClaimComparer()), Constants.IdentityServerAuthenticationType, JwtClaimTypes.Name, JwtClaimTypes.Role);
            return new ClaimsPrincipal(id);
        }
    }
}