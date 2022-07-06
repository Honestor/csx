using Framework.Core.Configurations;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using System.Reflection;

namespace Framework.IdentityServer4.Application
{
    public static class ApplicationConfigurationExtension
    {
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddMemoryStore()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            ConfigCore(application.Container);
            ConfigCookie(application.Container);
            return application;
        }

        /// <summary>
        /// 配置核心
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigCore(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        /// <summary>
        /// 配置cookie策略 http下google写入安全问题
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigCookie(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                {
                    if (cookieContext.CookieOptions.SameSite == SameSiteMode.None)
                    {
                        var userAgent = cookieContext.Context.Request.Headers["User-Agent"].ToString();
                        if (true)
                        {
                            cookieContext.CookieOptions.SameSite = SameSiteMode.Unspecified;
                        }
                    }
                };
                options.OnDeleteCookie = cookieContext =>
                {
                    if (cookieContext.CookieOptions.SameSite == SameSiteMode.None)
                    {
                        var userAgent = cookieContext.Context.Request.Headers["User-Agent"].ToString();
                        if (true)
                        {
                            cookieContext.CookieOptions.SameSite = SameSiteMode.Unspecified;
                        }
                    }
                };
            });
        }

        /// <summary>
        /// 写入测试用MemoryStore
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration AddMemoryStore(this ApplicationConfiguration application)
        {
            application.Container.AddSingleton(Config.IdentityResources);
            application.Container.AddTransient<IResourceStore, InMemoryResourcesStore>();

            application.Container.AddSingleton(Config.ApiScopes);
            application.Container.AddTransient<IResourceStore, InMemoryResourcesStore>();

            application.Container.AddSingleton(Config.Clients);
            application.Container.AddTransient<IClientStore, InMemoryClientStore>();

            application.Container.AddSingleton(new TestUserStore(TestUsers.Users));
            application.Container.AddTransient<IProfileService,TestUserProfileService>();
            application.Container.AddTransient<IResourceOwnerPasswordValidator, TestUserResourceOwnerPasswordValidator>();

            application.Container.AddTransient<ICorsPolicyService, InMemoryCorsPolicyService>();

            return application;
        }
    }
}
