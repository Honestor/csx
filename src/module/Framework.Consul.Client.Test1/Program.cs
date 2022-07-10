using Framework.AspNetCore.Mvc;
using Framework.Consul.Client;
using Framework.Core.Configurations;
using Framework.Json;
using Framework.Uow;
using Framework.Web.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .UseCore()
    .UseJson()
    .UseAspNetCore()
    .UseAspNetCoreMvc()
    .UseUnitOfWork()
    .UseConsulClient()
    .UseApplication()
    .LoadModules();

var app = builder.Build();

app.UseRouting();

app.UseConfiguredEndpoints();

app.Run("http://localhost:21313");
