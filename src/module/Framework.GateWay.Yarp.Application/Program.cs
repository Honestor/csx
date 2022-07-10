using Framework.Core.Configurations;
using Framework.GateWay.Yarp;

var builder = WebApplication.CreateBuilder(args);
builder.Services
       .UseCore()
       .UseYarp()
       .LoadModules();

var app = builder.Build();
app.UseRouting();
app.UseConfiguredEndpoints();
app.Run("http://localhost:6000");
