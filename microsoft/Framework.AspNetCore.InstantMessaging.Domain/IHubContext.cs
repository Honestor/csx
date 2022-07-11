namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IHubContext<THub> where THub : Hub
    {
        /// <summary>
        /// �ͻ���
        /// </summary>
        IHubClients Clients { get; }

        /// <summary>
        /// ��
        /// </summary>
        IGroupManager Groups { get; }
    }
}
