using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Consul.Client
{
    public class ConsulClientOptions
    {
        /// <summary>
        /// Consule客户端地址
        /// </summary>
        public string ConsulAddress { get; set; }

        /// <summary>
        /// 服务Ip
        /// </summary>
        public string ServiceIp { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 服务端口
        /// </summary>
        public int ServicePort { get; set; }
    }
}
