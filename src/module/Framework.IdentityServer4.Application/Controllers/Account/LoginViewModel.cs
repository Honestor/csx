namespace Framework.IdentityServer4.Application
{
    public class LoginViewModel : LoginInputModel
    {
        /// <summary>
        /// 是否记住登录 这里会做cookie持久化
        /// </summary>
        public bool AllowRememberLogin { get; set; } = true;

        /// <summary>
        /// 是否开启本地登录
        /// </summary>
        public bool EnableLocalLogin { get; set; } = true;

        /// <summary>
        /// 外部登录
        /// </summary>
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProvider>();

        /// <summary>
        /// 可用的外部登录
        /// </summary>
        public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !String.IsNullOrWhiteSpace(x.DisplayName));

        /// <summary>
        /// 是否只有外部登录
        /// </summary>
        public bool IsExternalLoginOnly => EnableLocalLogin == false && ExternalProviders?.Count() == 1;

        /// <summary>
        /// 外部登录scheme
        /// </summary>
        public string? ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
    }
}