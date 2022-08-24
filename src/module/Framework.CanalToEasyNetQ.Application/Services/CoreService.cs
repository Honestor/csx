using EasyNetQ;
using EasyNetQ.Topology;
using Framework.Canal;
using Framework.Canal.EasyNetQ.Configurations;
using Framework.Core.Dependency;
using Microsoft.Extensions.Options;
using System.Text;

namespace Framework.CanalToEasyNetQ.Application.Services
{
    public class CoreService:ISingleton
    {
        private CanalToEasyNetQOptions _canalToEasyNetQOptions;

        private Lazy<IBus> _bus;

        protected IBus Bus => _bus.Value;

        private CanalConsumer _canalConsumer;

        public CoreService(IOptionsMonitor<CanalToEasyNetQOptions> canalToEasyNetQOptions, CanalConsumer canalConsumer)
        {
            _canalToEasyNetQOptions = canalToEasyNetQOptions.CurrentValue;
            _bus = new Lazy<IBus>(() =>
            {
                return RabbitHutch.CreateBus(canalToEasyNetQOptions.CurrentValue.MqConnectionString);
            }, true);
            _canalConsumer = canalConsumer;
        }

        public void Start()
        {
            AsyncHelper.RunSync(() =>
            {
                return AutoConsumeAsync();
            });
        }


        private async Task AutoConsumeAsync()
        {
            foreach (var filter in _canalToEasyNetQOptions.Filters)
            {
                var exchange = await Bus.Advanced.ExchangeDeclareAsync(filter.TopicExchange.ExchangeName, ExchangeType.Topic, filter.TopicExchange.Durable, filter.TopicExchange.AutoDelete, CancellationToken.None);
                await _canalConsumer.ConsumeSingleAsync(filter.CanalFilter, changeData => {
                    Task.Run(async () =>
                    {
                        foreach (var changeItem in changeData)
                        {
                            await Bus.Advanced.PublishAsync(exchange, filter.TopicExchange.RouteKey, filter.TopicExchange.Mandatory, filter.TopicExchange.MessageProperties, Encoding.UTF8.GetBytes(changeItem));
                        }
                    });
                }, CancellationToken.None);
            }
        }

        public void Stop()
        {
            
           
        }
    }
}
