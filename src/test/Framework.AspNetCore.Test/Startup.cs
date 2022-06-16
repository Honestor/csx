using Framework.AspNetCore.Mvc;
using Framework.AspNetCore.Swagger;
using Framework.BlobStoring;
using Framework.BlobStoring.FileSystem;
using Framework.Core.Configurations;
using Framework.Dapper;
using Framework.Data.MySql;
using Framework.Data.Oralce;
using Framework.Excel.Npoi;
using Framework.ExceptionHandling;
using Framework.Json;
using Framework.Logging;
using Framework.Security;
using Framework.Uow;
using Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Framework.AspNetCore.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureFrameworkModules(services);
            services.AddLogging(loggingBuilder =>
          loggingBuilder.AddSerilog(dispose: true));
            ConfigureServiceModules();
        }

        /// <summary>
        /// 配置框架模块 记载必须的业务组件
        /// </summary>
        private void ConfigureFrameworkModules(IServiceCollection services)
        {
            services
            .UseCore()
            .UseLogging()
            .UseSecurity()
            .UseException()
            .UseJson()
            .UseUnitOfWork()
            .UseAspNetCore()
            .UseAspNetCoreMvc()
            .UseOracel()
            .UseDapper()
            .UseNpoiExcel()
            .UseBlobStoring()
            .UseFileSystemBlobStoring();
        }

        /// <summary>
        /// 配置业务模块
        /// </summary>
        private void ConfigureServiceModules()
        {
            ApplicationConfiguration.Current
               .UseApplication()
               .UseAspNetCoreSwagger();

            //自定义认证过滤器注入
            ApplicationConfiguration.Current.Container.Configure<MvcOptions>(mvcOptions =>
            {
                mvcOptions.Filters.AddService(typeof(AuthenticationFilter));
            });


            //加载所有模块
            ApplicationConfiguration.Current.LoadModules();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseExceptionHandling();
            app.UseUnitOfWork();
            app.UseConfiguredEndpoints();
            app.UseAspNetCoreSwagger();
        }
    }
}
