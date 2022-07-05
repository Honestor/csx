using Framework.Core.Configurations;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.IdentityServer4.Domain
{
    public static class ApplicationConfigurationExtension
    {
        public static ApplicationConfiguration UseIdentityServer4(this ApplicationConfiguration application)
        {
            var builder=application.Container
                .AddIdentityServer();

            //加入临时密钥
            builder.AddDeveloperSigningCredential();
            return application;
        }
    }
}
