using Framework.Consul.Client;
using Framework.Core.Configurations;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5001";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
builder.Services.Configure<CookiePolicyOptions>(options =>
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

builder.Services
    .UseCore()
    .UseConsulClient()
    .LoadModules();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});
app.Run("http://localhost:5003");
