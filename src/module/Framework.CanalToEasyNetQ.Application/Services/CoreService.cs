using Framework.Canal;
using Framework.Canal.EasyNetQ.Configurations;
using Framework.Core.Dependency;
using Framework.EasyNetQ;
using Microsoft.Extensions.Options;

namespace Framework.CanalToEasyNetQ.Application.Services
{
    public class CoreService:ISingleton
    {
        private CanalToEasyNetQOptions _canalToEasyNetQOptions;

        private CanalConsumer _canalConsumer;

        private MqPublisher _mqPublisher;

        public CoreService(IOptionsMonitor<CanalToEasyNetQOptions> canalToEasyNetQOptions, CanalConsumer canalConsumer, MqPublisher mqPublisher)
        {
            _canalToEasyNetQOptions = canalToEasyNetQOptions.CurrentValue;
            _canalConsumer = canalConsumer;
            _mqPublisher = mqPublisher;
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
                await _canalConsumer.ConsumeSingleAsync(filter.CanalFilter, changeData => {
                    Task.Run(async () =>
                    {
                        foreach (var changeItem in changeData)
                        {
                            await _mqPublisher.PublishAsync(filter.TopicExchange, filter.TopicExchange.RouteKey, changeItem);
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
