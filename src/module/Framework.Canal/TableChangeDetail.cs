using System.Collections.Generic;

namespace Framework.Canal
{
    /// <summary>
    /// 表变更细节
    /// </summary>
    public class TableChangeDetail<T> where T : class,new()
    {
        /// <summary>
        /// binlog 文件名称
        /// </summary>
        public string BinLogFileName { get; set; }

        /// <summary>
        /// 数据库
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 变更数据
        /// </summary>
        public T Data { get; set; }
    }
}
