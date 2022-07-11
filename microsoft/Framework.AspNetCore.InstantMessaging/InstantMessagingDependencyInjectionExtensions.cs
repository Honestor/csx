using Framework.AspNetCore.Connections;
using Framework.AspNetCore.InstantMessaging.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using Framework.AspNetCore.InstantMessaging.Protocols.Json;

namespace Framework.AspNetCore.InstantMessaging.Application
{
    public static class InstantMessagingDependencyInjectionExtensions
    {
        /// <summary>
        /// 启用即时通信应用模块
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IInstantMessagingServerBuilder UseInstantMessagingApplication(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            //关闭websocket自带的心跳检测,采用自己的
            services.Configure<Microsoft.AspNetCore.Builder.WebSocketOptions>(o => o.KeepAliveInterval = TimeSpan.Zero);

            //引入连接管理模块
            services.AddHttpConnections();

            //hub默认配置
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HubOptions>, HubOptionsSetup>());

            //即时通信核心模块
            return 
                services.AddInstantMessagingCore()
                .AddJsonProtocol() //框架启动默认需要Json协议,所以不能移除
                .AddMessagePackProtocol(); //MessagePack 消息压缩,节省空间
        }
    }
}
