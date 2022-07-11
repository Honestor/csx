namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub 泛型配置
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class HubOptions<THub> : HubOptions where THub : Hub
    {
        /// <summary>
        /// 用户有没有修改配置 深拷贝一个副本
        /// </summary>
        internal bool UserHasSetValues { get; set; }
    }
}
