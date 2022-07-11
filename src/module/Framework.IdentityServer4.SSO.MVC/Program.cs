using Framework.Consul.Client;
using Framework.Core.Configurations;
using Framework.IdentityServer4.SSO.MVC;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

//����Ӧ�ó���
builder.Services
    .UseCore()
    .UseConsulClient()
    .UseApplication()
    .LoadModules();
//����Ӧ�ó������
var app = builder.Build();

//�����м��
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute()
        .RequireAuthorization();
});
//�����м������

app.Run("http://localhost:5002");
