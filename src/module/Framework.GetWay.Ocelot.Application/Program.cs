using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Framework.GetWay.Ocelot;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .UseCore()
    .UseOcelotWithConsul()
    .LoadModules();

//¿çÓò°×Ãûµ¥
var allOrigins = builder.Services.GetConfiguration().GetSection("CorsOptions:AllowOrigins").GetChildren().Select(s => s.Value).ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder
        .WithOrigins(allOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});


var app = builder.Build();
app.UseCors("CorsPolicy");
app.UseOcelot().Wait();

app.Run("http://localhost:45345");
