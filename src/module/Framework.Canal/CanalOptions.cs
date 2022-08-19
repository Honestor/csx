using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Canal
{
    public class CanalOptions
    {
        /// <summary>
        /// Canal地址
        /// </summary>
        public string HostAdress { get; set; }

        /// <summary>
        /// canal端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 客户端Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }
    }
}
