using System.Collections.Generic;

namespace Framework.AspNetCore.Connections.Common
{
    public class NegotiationResponse
    {
        /// <summary>
        /// 协商异常
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// 连接
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 访问令牌
        /// </summary>
        public string AccessToken { get; set; }

       /// <summary>
       /// 连接Id
       /// </summary>
        public string ConnectionId { get; set; }

       
        public string ConnectionToken { get; set; }

        /// <summary>
        /// 客户端传入版本
        /// </summary>
        public int Version { get; set; }

        public IList<AvailableTransport> AvailableTransports { get; set; }
    }
}
