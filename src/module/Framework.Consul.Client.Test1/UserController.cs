using Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Framework.Consul.Client.Test1
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private ConsulClientOptions _consulClientOptions;
        public UserController(IOptionsMonitor<ConsulClientOptions> consulClientOptions)
        {
            _consulClientOptions = consulClientOptions.CurrentValue;
        }

        [HttpGet]
        [Route("getwaytest")]
        public IActionResult GetWayTest()
        {
            return Ok();
        }


    }
}
