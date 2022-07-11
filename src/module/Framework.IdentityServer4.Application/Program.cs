using Framework.Consul.Client;
using Framework.Core.Configurations;
using Framework.IdentityServer4.Application;
using Framework.IdentityServer4.Domain;
using IdentityServer4.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .UseCore()
    .UseIdentityServer4()
    .UseApplication()
    .UseConsulClient()
    .LoadModules();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseCookiePolicy();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});
app.Run("http://localhost:5001");
