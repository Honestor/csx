using Framework.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Canal
{
    public class CanalConsumer<T> where T: TableChangeModelBase, new()
    {
        private CanalConsumer _canalConsumer;
        private ILogger<CanalConsumer<T>> _logger;
        private IJsonSerializer _jsonSerializer;

        public CanalConsumer(ILogger<CanalConsumer<T>> logger, CanalConsumer canalConsumer, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _canalConsumer = canalConsumer;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// 单机消费 not zookeeper集群
        /// </summary>
        /// <returns></returns>
        public async Task ConsumeSingleAsync(string filter,Action<List<TableChangeDetail<T>>> callback, CancellationToken cancellationToken=default)
        {
            await _canalConsumer.ConsumeSingleAsync(filter, content =>
            {
                var result = new List<TableChangeDetail<T>>();
                foreach (var item in content)
                {
                    try
                    {
                        result.Add(_jsonSerializer.Deserialize<TableChangeDetail<T>>(item));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"订阅消息:{item}序列化异常,异常信息:{ex.Message},堆栈:{ex.StackTrace}");
                    }
                    if(result.Count>0)
                        callback?.Invoke(result);
                }
                
            }, cancellationToken);
        }
    }
}
