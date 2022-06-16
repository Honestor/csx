namespace Framework.Core.Data
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public class DbOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; }
    }
}
