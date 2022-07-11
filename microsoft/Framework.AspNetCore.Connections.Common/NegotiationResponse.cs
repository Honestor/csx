using System.Collections.Generic;

namespace Framework.AspNetCore.Connections.Common
{
    public class NegotiationResponse
    {
        /// <summary>
        /// Э���쳣
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// ����
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public string AccessToken { get; set; }

       /// <summary>
       /// ����Id
       /// </summary>
        public string ConnectionId { get; set; }

       
        public string ConnectionToken { get; set; }

        /// <summary>
        /// �ͻ��˴���汾
        /// </summary>
        public int Version { get; set; }

        public IList<AvailableTransport> AvailableTransports { get; set; }
    }
}
