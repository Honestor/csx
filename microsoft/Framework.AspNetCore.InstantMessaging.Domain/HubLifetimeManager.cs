using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub�������ڹ���
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public abstract class HubLifetimeManager<THub> where THub : Hub
    {
        /// <summary>
        /// д������������
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task SendAllAsync(string methodName, object?[]? args, CancellationToken cancellationToken = default);

        /// <summary>
        /// ���Ӽ��뵽group��
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="groupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task SendGroupAsync(string groupName, string methodName, object?[]? args, CancellationToken cancellationToken = default);
    }
}
