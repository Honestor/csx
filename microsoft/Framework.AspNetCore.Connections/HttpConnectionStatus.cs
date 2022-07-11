namespace Framework.AspNetCore.Connections
{
    /// <summary>
    /// http连接状态
    /// </summary>
    internal enum HttpConnectionStatus
    {
        /// <summary>
        /// 待激活
        /// </summary>
        Inactive,
        /// <summary>
        /// 活动
        /// </summary>
        Active,
        /// <summary>
        /// 以释放
        /// </summary>
        Disposed
    }
}
