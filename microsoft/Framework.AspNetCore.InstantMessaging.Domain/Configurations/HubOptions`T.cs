namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub ��������
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class HubOptions<THub> : HubOptions where THub : Hub
    {
        /// <summary>
        /// �û���û���޸����� ���һ������
        /// </summary>
        internal bool UserHasSetValues { get; set; }
    }
}
