using EasyNetQ;
using EasyNetQ.Topology;
using Framework.Core.Dependency;
using Framework.EasyNetQ.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.EasyNetQ
{
    public class MqPublisher:ISingleton
    {
        private Lazy<IBus> _bus;

        private IBus Bus => _bus.Value;

        public MqPublisher(IOptionsMonitor<EasyNetQOptions> optionsMonitor)
        {
            _bus = new Lazy<IBus>(() =>
            {
                return RabbitHutch.CreateBus(optionsMonitor.CurrentValue.ConnectionString);
            }, true);
        }

        /// <summary>
        /// Topic发送消息
        /// </summary>
        /// <param name="topicExchange"></param>
        /// <param name="routeKey"></param>
        /// <param name="publishContent"></param>
        /// <returns></returns>
        public async Task PublishAsync(TopicExchange topicExchange,string routeKey,string publishContent, CancellationToken cancellationToken=default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidTopicExchange(topicExchange);
            if (string.IsNullOrEmpty(routeKey))
            {
                throw new Exception("routekey can not be null");
            }

            if (string.IsNullOrEmpty(publishContent))
            {
                throw new Exception("publish content can not be null");
            }
            var exchange = await Bus.Advanced.ExchangeDeclareAsync(topicExchange.Name, ExchangeType.Topic, topicExchange.Durable, topicExchange.AutoDelete, cancellationToken);
            await Bus.Advanced.PublishAsync(exchange, routeKey, topicExchange.Mandatory, topicExchange.MessageProperties, Encoding.UTF8.GetBytes(publishContent));
        }

        /// <summary>
        /// 校验Topic模式发送参数
        /// </summary>
        /// <param name="topicExchange"></param>
        /// <exception cref="Exception"></exception>
        private void ValidTopicExchange(TopicExchange topicExchange)
        {
            if (string.IsNullOrEmpty(topicExchange.Name))
            {
                throw new Exception("exchange name can not be null");
            }
        }
    }
}
