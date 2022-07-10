using Framework.Core.Configurations;
using Framework.GetWay.Ocelot;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .UseCore()
    .UseOcelotWithConsul()
    .LoadModules();

var app = builder.Build();

app.UseOcelot();

app.Run("http://localhost:45345");
