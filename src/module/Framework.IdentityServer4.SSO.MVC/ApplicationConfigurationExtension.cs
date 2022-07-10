using Framework.Core.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;

namespace Framework.IdentityServer4.SSO.MVC
{
    public static class ApplicationConfigurationExtension
    {
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            ConfigCore(application.Container);
            ConfigAuthentication(application.Container);
            ConfigCookie(application.Container);
            return application;
        }

        /// <summary>
        /// 配置核心
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigCore(IServiceCollection services)
        {
            services.AddControllers();
        }

        /// <summary>
        /// 配置认证相关
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigAuthentication(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = " http://localhost:45345/getway/";
                options.RequireHttpsMetadata = false;
                options.ClientId = "mvc";
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.Scope.Add("api1");
                options.SaveTokens = false;
            });
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
    }
}
