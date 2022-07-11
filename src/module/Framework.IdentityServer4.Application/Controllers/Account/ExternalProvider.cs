namespace Framework.IdentityServer4.Application
{
    /// <summary>
    /// 外部登录
    /// </summary>
    public class ExternalProvider
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 认证方式
        /// </summary>
        public string AuthenticationScheme { get; set; }
    }
}