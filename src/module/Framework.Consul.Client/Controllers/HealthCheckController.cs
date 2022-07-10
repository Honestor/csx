using Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Consul.Client.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        private ConsulClientOptions _consulClientOptions;
        public HealthCheckController(IOptionsMonitor<ConsulClientOptions> consulClientOptions)
        {
            _consulClientOptions = consulClientOptions.CurrentValue;
        }

        /// <summary>
        /// 健康检测
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        { 
            return Ok();
        }

        /// <summary>
        /// 模拟服务发现
        /// </summary>
        [HttpGet]
        [Route("consul")]
        public async Task<IActionResult> ReuestConsul()
        {
            using (var consulClient = new ConsulClient(x => x.Address = new Uri(_consulClientOptions.ConsulAddress)))
            {
                var services = (await consulClient.Catalog.Service(_consulClientOptions.ServiceName)).Response;
                if (services != null && services.Any())
                {
                    return Content($"当前服务数:{services.Count()}");
                }
                else {
                    return Content($"未检测到服务,请检查客户端consul配置");
                }
            }
        }
    }
}
