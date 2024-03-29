﻿using Framework.AspNetCore.ExceptionHandling;
using Framework.Core.Dependency;
using Framework.Core.Exceptions;
using Framework.ExceptionHandling;
using Framework.ExceptionHandling.Http;
using Framework.Http;
using Framework.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;

namespace Framework.Web.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// 异常中间件
    /// </summary>
    public class ExceptionHandlingMiddleware : IMiddleware, ITransient
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        private readonly Func<object, Task> _clearCacheHeadersDelegate;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;

            _clearCacheHeadersDelegate = ClearCacheHeaders;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                //如果响应开始,只能停止请求
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("An exception occurred, but response has already started!");
                    throw;
                }

                await HandleAndWrapException(context, ex);
                return;
            }
        }

        private async Task HandleAndWrapException(HttpContext httpContext, Exception exception)
        {
            _logger.LogException(exception);

            var errorInfoConverter = httpContext.RequestServices.GetRequiredService<IExceptionToErrorInfoConverter>();
            var statusCodeFinder = httpContext.RequestServices.GetRequiredService<IHttpExceptionStatusCodeFinder>();
            var jsonSerializer = httpContext.RequestServices.GetRequiredService<IJsonSerializer>();
            var options = httpContext.RequestServices.GetRequiredService<IOptions<ExceptionHandlingOptions>>().Value;

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = (int)statusCodeFinder.GetStatusCode(httpContext, exception);
            httpContext.Response.OnStarting(_clearCacheHeadersDelegate, httpContext.Response);
            httpContext.Response.Headers.Add(FrameworkHttpConsts.ErrorFormat, "true");

            if (options.UseStandardOutput)
            {
                await httpContext.Response.WriteAsync(
                 jsonSerializer.Serialize(
                     new RemoteServiceErrorResponse(
                         errorInfoConverter.Convert(exception, options.SendExceptionsDetailsToClients)
                     )
                 )
             );
            }
            

            await httpContext
                .RequestServices
                .GetRequiredService<IExceptionNotifier>()
                .NotifyAsync(
                    new ExceptionNotificationContext(exception, httpContext.Items)
                );
        }


        /// <summary>
        /// 清楚缓存头
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private Task ClearCacheHeaders(object state)
        {
            var response = (HttpResponse)state;

            response.Headers[HeaderNames.CacheControl] = "no-cache";
            response.Headers[HeaderNames.Pragma] = "no-cache";
            response.Headers[HeaderNames.Expires] = "-1";
            response.Headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }
}
