using Framework.Consul.Client;
using Framework.Core.Configurations;
using Framework.IdentityServer4.SSO.MVC;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

//配置应用程序
builder.Services
    .UseCore()
    .UseConsulClient()
    .UseApplication()
    .LoadModules();
//配置应用程序结束
var app = builder.Build();

//引用中间件
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute()
        .RequireAuthorization();
});
//引用中间件结束

app.Run("http://localhost:5002");
