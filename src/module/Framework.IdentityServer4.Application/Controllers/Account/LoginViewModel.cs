namespace Framework.IdentityServer4.Application
{
    public class LoginViewModel : LoginInputModel
    {
        /// <summary>
        /// �Ƿ��ס��¼ �������cookie�־û�
        /// </summary>
        public bool AllowRememberLogin { get; set; } = true;

        /// <summary>
        /// �Ƿ������ص�¼
        /// </summary>
        public bool EnableLocalLogin { get; set; } = true;

        /// <summary>
        /// �ⲿ��¼
        /// </summary>
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProvider>();

        /// <summary>
        /// ���õ��ⲿ��¼
        /// </summary>
        public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !String.IsNullOrWhiteSpace(x.DisplayName));

        /// <summary>
        /// �Ƿ�ֻ���ⲿ��¼
        /// </summary>
        public bool IsExternalLoginOnly => EnableLocalLogin == false && ExternalProviders?.Count() == 1;

        /// <summary>
        /// �ⲿ��¼scheme
        /// </summary>
        public string? ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
    }
}