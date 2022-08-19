using Framework.AspNetCore.Mvc;
using Framework.Core.Configurations;
using Framework.ElasticSearch.Application;
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
    .UseApplication()
    .LoadModules();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseConfiguredEndpoints();

app.Run();
