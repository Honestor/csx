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
        /// ���ü�ʱͨ��Ӧ��ģ��
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IInstantMessagingServerBuilder UseInstantMessagingApplication(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            //�ر�websocket�Դ����������,�����Լ���
            services.Configure<Microsoft.AspNetCore.Builder.WebSocketOptions>(o => o.KeepAliveInterval = TimeSpan.Zero);

            //�������ӹ���ģ��
            services.AddHttpConnections();

            //hubĬ������
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HubOptions>, HubOptionsSetup>());

            //��ʱͨ�ź���ģ��
            return 
                services.AddInstantMessagingCore()
                .AddJsonProtocol() //�������Ĭ����ҪJsonЭ��,���Բ����Ƴ�
                .AddMessagePackProtocol(); //MessagePack ��Ϣѹ��,��ʡ�ռ�
        }
    }
}
