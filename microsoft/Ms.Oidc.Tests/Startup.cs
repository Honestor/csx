using IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ms.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Ms.Oidc.Tests
{
    public class ConcurrentDataList<TEntity> where TEntity : class
    {
        private static ReaderWriterLockSlim _cacheLock;
        private static List<TEntity> _innerCache;
        static ConcurrentDataList()
        {
            _innerCache = new List<TEntity>();
            _cacheLock = new ReaderWriterLockSlim();
        }

        public List<TEntity> Read()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _innerCache;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
    }

    public class A
    {

    }

    public class B
    {

    }

    public class C
    { 
        
    }

    public class Startup
    {
        void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (true)
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var a=new ConcurrentDataList<A>();
            var b = new ConcurrentDataList<B>();
            var c = new ConcurrentDataList<C>();
            services.AddControllersWithViews();
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = "http://localhost:45345/identityserver/";
                options.RequireHttpsMetadata = false;
                options.ClientId = "mvc";
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Events.OnRedirectToIdentityProvider += (context) =>
                {
                    (context as RedirectContext).ProtocolMessage.RedirectUri = $"http://localhost:45345/mvc/signin-oidc";
                    return Task.CompletedTask;
                };
            });
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
            services.AddSingleton<PolicyEvaluatorTest>();
            services.AddTransient<IAuthorizationPolicyProvider,TestAuthorizationPolicyProvider>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
            app.UseRouting();
            app.UseCookiePolicy();
            app.UseMiddleware<TestAuthenticationMiddleware>();
            app.UseMiddleware<TestMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute()
                    .RequireAuthorization();
            });
        }
    }


    public class TestAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next item in the middleware pipeline.</param>
        /// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/>.</param>
        public TestAuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (schemes == null)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            _next = next;
            Schemes = schemes;
        }

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationSchemeProvider"/>.
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; set; }

        /// <summary>
        /// Invokes the middleware performing authentication.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        public async Task Invoke(HttpContext context)
        {
            context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                OriginalPath = context.Request.Path,
                OriginalPathBase = context.Request.PathBase
            });

            // Give any IAuthenticationRequestHandler schemes a chance to handle the request
            var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            foreach (var scheme in await Schemes.GetRequestHandlerSchemesAsync())
            {
                //这里oidc流程是远程调用的一种,所以会执行OpenIdConnectHandler实例的HandleRequestAsync方法
                var handler = await handlers.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;
                if (handler != null && await handler.HandleRequestAsync())
                {
                    return;
                }
            }

            var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultAuthenticate != null)
            {
                var result = await context.AuthenticateAsync(defaultAuthenticate.Name);
                if (result?.Principal != null)
                {
                    context.User = result.Principal;
                }
            }

            await _next(context);
        }
    }

    public class TestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private const string SuppressUseHttpContextAsAuthorizationResource = "Microsoft.AspNetCore.Authorization.SuppressUseHttpContextAsAuthorizationResource";
        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="policyProvider">The <see cref="IAuthorizationPolicyProvider"/>.</param>
        public TestMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
        }
        private const string AuthorizationMiddlewareInvokedWithEndpointKey = "__AuthorizationMiddlewareWithEndpointInvoked";
        private static readonly object AuthorizationMiddlewareWithEndpointInvokedValue = new object();
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var endpoint = context.GetEndpoint();

            if (endpoint != null)
            {
                context.Items[AuthorizationMiddlewareInvokedWithEndpointKey] = AuthorizationMiddlewareWithEndpointInvokedValue;
            }

            // IMPORTANT: Changes to authorization logic should be mirrored in MVC's AuthorizeFilter
            var authorizeData = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? Array.Empty<IAuthorizeData>();
            var policy = await TestAuthorizationPolicy.CombineAsync(_policyProvider, authorizeData);
            if (policy == null)
            {
                await _next(context);
                return;
            }

            // Policy evaluator has transient lifetime so it fetched from request services instead of injecting in constructor
            var policyEvaluator = context.RequestServices.GetRequiredService<PolicyEvaluatorTest>();

            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context);

            // Allow Anonymous skips all authorization
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            object? resource;
            if (AppContext.TryGetSwitch(SuppressUseHttpContextAsAuthorizationResource, out var useEndpointAsResource) && useEndpointAsResource)
            {
                resource = endpoint;
            }
            else
            {
                resource = context;
            }

            var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, resource);
            await new AuthorizationMiddlewareResultHandler().HandleAsync(_next, context, policy, authorizeResult);
        }
    }

    public class AuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        /// <inheritdoc />
        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Challenged)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }

                return;
            }
            else if (authorizeResult.Forbidden)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ForbidAsync(scheme);
                    }
                }
                else
                {
                    await context.ForbidAsync();
                }

                return;
            }

            await next(context);
        }
    }

    public class AuthenticationService 
    {
        public IAuthenticationSchemeProvider Schemes { get; }

        public IAuthenticationHandlerProvider Handlers { get; }

        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers)
        {
            Schemes = schemes;
            Handlers = handlers;
        }

        public virtual async Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            if (scheme == null)
            {
                var defaultChallengeScheme = await Schemes.GetDefaultChallengeSchemeAsync();
                scheme = defaultChallengeScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultChallengeScheme found. The default schemes can be set using either AddAuthentication(string defaultScheme) or AddAuthentication(Action<AuthenticationOptions> configureOptions).");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme);
            if (handler == null)
            {
                //throw await CreateMissingHandlerException(scheme);
            }

            await handler.ChallengeAsync(properties);
        }
    }

    public class PolicyEvaluatorTest
    {
        private readonly IAuthorizationService _authorization;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="authorization">The authorization service.</param>
        public PolicyEvaluatorTest(IAuthorizationService authorization)
        {
            _authorization = authorization;
        }

        /// <summary>
        /// Does authentication for <see cref="AuthorizationPolicy.AuthenticationSchemes"/> and sets the resulting
        /// <see cref="ClaimsPrincipal"/> to <see cref="HttpContext.User"/>.  If no schemes are set, this is a no-op.
        /// </summary>
        /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns><see cref="AuthenticateResult.Success"/> unless all schemes specified by <see cref="AuthorizationPolicy.AuthenticationSchemes"/> failed to authenticate.  </returns>
        public virtual async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
        {
            if (policy.AuthenticationSchemes != null && policy.AuthenticationSchemes.Count > 0)
            {
                ClaimsPrincipal? newPrincipal = null;
                foreach (var scheme in policy.AuthenticationSchemes)
                {
                    var result = await context.AuthenticateAsync(scheme);
                    if (result != null && result.Succeeded)
                    {
                        
                        newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result.Principal);
                    }
                }

                if (newPrincipal != null)
                {
                    context.User = newPrincipal;
                    return AuthenticateResult.Success(new AuthenticationTicket(newPrincipal, string.Join(";", policy.AuthenticationSchemes)));
                }
                else
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                    return AuthenticateResult.NoResult();
                }
            }

            return (context.User?.Identity?.IsAuthenticated ?? false)
                ? AuthenticateResult.Success(new AuthenticationTicket(context.User, "context.User"))
                : AuthenticateResult.NoResult();
        }

        /// <summary>
        /// Attempts authorization for a policy using <see cref="IAuthorizationService"/>.
        /// </summary>
        /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
        /// <param name="authenticationResult">The result of a call to <see cref="AuthenticateAsync(AuthorizationPolicy, HttpContext)"/>.</param>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="resource">
        /// An optional resource the policy should be checked with.
        /// If a resource is not required for policy evaluation you may pass null as the value.
        /// </param>
        /// <returns>Returns <see cref="PolicyAuthorizationResult.Success"/> if authorization succeeds.
        /// Otherwise returns <see cref="PolicyAuthorizationResult.Forbid(AuthorizationFailure)"/> if <see cref="AuthenticateResult.Succeeded"/>, otherwise
        /// returns  <see cref="PolicyAuthorizationResult.Challenge"/></returns>
        public virtual async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object? resource)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            var result = await _authorization.AuthorizeAsync(context.User, resource, policy);
            if (result.Succeeded)
            {
                return PolicyAuthorizationResult.Success();
            }

            // If authentication was successful, return forbidden, otherwise challenge
            return (authenticationResult.Succeeded)
                ? PolicyAuthorizationResult.Forbid(result.Failure)
                : PolicyAuthorizationResult.Challenge();
        }
    }

    internal static class SecurityHelper
    {
        /// <summary>
        /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
        /// Merges a new claims principal, placing all new identities first, and eliminating
        /// any empty unauthenticated identities from context.User
        /// </summary>
        /// <param name="existingPrincipal">The <see cref="ClaimsPrincipal"/> containing existing <see cref="ClaimsIdentity"/>.</param>
        /// <param name="additionalPrincipal">The <see cref="ClaimsPrincipal"/> containing <see cref="ClaimsIdentity"/> to be added.</param>
        public static ClaimsPrincipal MergeUserPrincipal(ClaimsPrincipal? existingPrincipal, ClaimsPrincipal? additionalPrincipal)
        {
            var newPrincipal = new ClaimsPrincipal();

            // New principal identities go first
            if (additionalPrincipal != null)
            {
                newPrincipal.AddIdentities(additionalPrincipal.Identities);
            }

            // Then add any existing non empty or authenticated identities
            if (existingPrincipal != null)
            {
                newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Any()));
            }
            return newPrincipal;
        }
    }

    public class TestAuthorizationPolicy
    {
        /// <summary>
        /// Creates a new instance of <see cref="AuthorizationPolicy"/>.
        /// </summary>
        /// <param name="requirements">
        /// The list of <see cref="IAuthorizationRequirement"/>s which must succeed for
        /// this policy to be successful.
        /// </param>
        /// <param name="authenticationSchemes">
        /// The authentication schemes the <paramref name="requirements"/> are evaluated against.
        /// </param>
        public TestAuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            if (authenticationSchemes == null)
            {
                throw new ArgumentNullException(nameof(authenticationSchemes));
            }

            if (!requirements.Any())
            {
                throw new InvalidOperationException("");
            }
            Requirements = new List<IAuthorizationRequirement>(requirements).AsReadOnly();
            AuthenticationSchemes = new List<string>(authenticationSchemes).AsReadOnly();
        }

        /// <summary>
        /// Gets a readonly list of <see cref="IAuthorizationRequirement"/>s which must succeed for
        /// this policy to be successful.
        /// </summary>
        public IReadOnlyList<IAuthorizationRequirement> Requirements { get; }

        /// <summary>
        /// Gets a readonly list of the authentication schemes the <see cref="AuthorizationPolicy.Requirements"/> 
        /// are evaluated against.
        /// </summary>
        public IReadOnlyList<string> AuthenticationSchemes { get; }

        /// <summary>
        /// Combines the specified <see cref="AuthorizationPolicy"/> into a single policy.
        /// </summary>
        /// <param name="policies">The authorization policies to combine.</param>
        /// <returns>
        /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
        /// specified <paramref name="policies"/>.
        /// </returns>
        public static AuthorizationPolicy Combine(params AuthorizationPolicy[] policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            return Combine((IEnumerable<AuthorizationPolicy>)policies);
        }

        /// <summary>
        /// Combines the specified <see cref="AuthorizationPolicy"/> into a single policy.
        /// </summary>
        /// <param name="policies">The authorization policies to combine.</param>
        /// <returns>
        /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
        /// specified <paramref name="policies"/>.
        /// </returns>
        public static AuthorizationPolicy Combine(IEnumerable<AuthorizationPolicy> policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            var builder = new AuthorizationPolicyBuilder();
            foreach (var policy in policies)
            {
                builder.Combine(policy);
            }
            return builder.Build();
        }

        /// <summary>
        /// Combines the <see cref="AuthorizationPolicy"/> provided by the specified
        /// <paramref name="policyProvider"/>.
        /// </summary>
        /// <param name="policyProvider">A <see cref="IAuthorizationPolicyProvider"/> which provides the policies to combine.</param>
        /// <param name="authorizeData">A collection of authorization data used to apply authorization to a resource.</param>
        /// <returns>
        /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
        /// authorization policies provided by the specified <paramref name="policyProvider"/>.
        /// </returns>
        public static async Task<AuthorizationPolicy?> CombineAsync(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authorizeData)
        {
            if (policyProvider == null)
            {
                throw new ArgumentNullException(nameof(policyProvider));
            }

            if (authorizeData == null)
            {
                throw new ArgumentNullException(nameof(authorizeData));
            }

            // Avoid allocating enumerator if the data is known to be empty
            var skipEnumeratingData = false;
            if (authorizeData is IList<IAuthorizeData> dataList)
            {
                skipEnumeratingData = dataList.Count == 0;
            }

            AuthorizationPolicyBuilder? policyBuilder = null;

            //如果控制器方法设置了Authorize特性
            if (!skipEnumeratingData)
            {
                //遍历控制器方法的Authorize特性
                foreach (var authorizeDatum in authorizeData)
                {
                    if (policyBuilder == null)
                    {
                        policyBuilder = new AuthorizationPolicyBuilder();
                    }

                    var useDefaultPolicy = true;


                    /*
                     * 主要功能：如果控制器方法打了Authorize特性,且Policy自定义策略有值,则从配置中获取自定义策略,如果配置中存在,则将值写入到AuthorizationPolicyBuilder实例中
                     * */

                    //如果自定义策略不为空
                    if (!string.IsNullOrWhiteSpace(authorizeDatum.Policy))
                    {
                        //如果没有重新IAuthorizationPolicyProvider,默认从AuthorizationOptions配置中获取自定义授权策略
                        //增加自定义授权策略通过配置AuthorizationOptions来增加,具体通过AuthorizationOptions的AddPolicy方法,来添加自定义授权策略.
                        var policy = await policyProvider.GetPolicyAsync(authorizeDatum.Policy);
                        if (policy == null)
                        {
                            throw new InvalidOperationException("");
                        }

                        //将从配置中的自定义授权策略中获取到的认证方案集合和Requirements集合转换成AuthorizationPolicyBuilder实例
                        policyBuilder.Combine(policy);
                        useDefaultPolicy = false;
                    }

                    //判断角色授权策略是否为空
                    var rolesSplit = authorizeDatum.Roles?.Split(',');
                    if (rolesSplit?.Length > 0)
                    {
                        //将角色策略集合添加到AuthorizationPolicyBuilder实例中的Requirements属性中
                        var trimmedRolesSplit = rolesSplit.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim());
                        policyBuilder.RequireRole(trimmedRolesSplit);
                        useDefaultPolicy = false;
                    }

                    //判断认证方案是否为空
                    var authTypesSplit = authorizeDatum.AuthenticationSchemes?.Split(',');
                    if (authTypesSplit?.Length > 0)
                    {
                        //将认证方案写入到AuthorizationPolicyBuilder实例中的AuthenticationSchemes属性中
                        foreach (var authType in authTypesSplit)
                        {
                            if (!string.IsNullOrWhiteSpace(authType))
                            {
                                policyBuilder.AuthenticationSchemes.Add(authType.Trim());
                            }
                        }
                    }

                    //如果控制器方法没有指定任何的自定义策略、角色策略、认证方案
                    if (useDefaultPolicy)
                    {
                        //采用默认的AuthorizationOptions配置的默认策略.接下去的文章会介绍
                        policyBuilder.Combine(await policyProvider.GetDefaultPolicyAsync());
                    }
                }
            }

            // 如果没有任何策略,则采用Fallback策略
            if (policyBuilder == null)
            {
                var fallbackPolicy = await policyProvider.GetFallbackPolicyAsync();
                if (fallbackPolicy != null)
                {
                    return fallbackPolicy;
                }
            }

            //将policyBuilder转换成police实例返回
            return policyBuilder?.Build();
        }
    }

    public class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;
        private Task<AuthorizationPolicy>? _cachedDefaultPolicy;
        private Task<AuthorizationPolicy?>? _cachedFallbackPolicy;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultAuthorizationPolicyProvider"/>.
        /// </summary>
        /// <param name="options">The options used to configure this instance.</param>
        public TestAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        /// <summary>
        /// Gets the default authorization policy.
        /// </summary>
        /// <returns>The default authorization policy.</returns>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            if (_cachedDefaultPolicy == null || _cachedDefaultPolicy.Result != _options.DefaultPolicy)
            {
                _cachedDefaultPolicy = Task.FromResult(_options.DefaultPolicy);
            }

            return _cachedDefaultPolicy;
        }

        /// <summary>
        /// Gets the fallback authorization policy.
        /// </summary>
        /// <returns>The fallback authorization policy.</returns>
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            if (_cachedFallbackPolicy == null || _cachedFallbackPolicy.Result != _options.FallbackPolicy)
            {
                _cachedFallbackPolicy = Task.FromResult(_options.FallbackPolicy);
            }

            return _cachedFallbackPolicy;
        }

        /// <summary>
        /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
        /// </summary>
        /// <param name="policyName">The policy name to retrieve.</param>
        /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
        public virtual Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // MVC caches policies specifically for this class, so this method MUST return the same policy per
            // policyName for every request or it could allow undesired access. It also must return synchronously.
            // A change to either of these behaviors would require shipping a patch of MVC as well.
            return Task.FromResult(_options.GetPolicy(policyName));
        }
    }
}
