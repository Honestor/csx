using CanalSharp.Connections;
using Framework.Core.Dependency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Framework.Canal
{
    public class CanalConnectionFactory:ISingleton
    {
        private ILoggerFactory _loggerFactory;
        private CanalOptions _canalOptions;
        public CanalConnectionFactory(ILoggerFactory loggerFactory,IOptionsMonitor<CanalOptions> _optionsMonitor)
        {
            _loggerFactory = loggerFactory;
            _canalOptions = _optionsMonitor.CurrentValue;
        }

        /// <summary>
        /// 创建单机连接
        /// </summary>
        /// <param name="filter">订阅的相关表 默认订阅所有的表</param>
        /// <returns></returns>
        public async Task<SimpleCanalConnection> CreateSingleAsync()
        {
            var options = new SimpleCanalOptions(_canalOptions.HostAdress, _canalOptions.Port, _canalOptions.Id) { UserName = _canalOptions.UserName??"canal", Password = _canalOptions.Password??"canal" };
            var connection = new SimpleCanalConnection(options, _loggerFactory.CreateLogger<SimpleCanalConnection>());
            await connection.ConnectAsync();
            return connection;
        }
    }
}
