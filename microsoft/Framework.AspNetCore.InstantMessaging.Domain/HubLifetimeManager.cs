using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub生命周期管理
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public abstract class HubLifetimeManager<THub> where THub : Hub
    {
        /// <summary>
        /// 写入连接上下文
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task SendAllAsync(string methodName, object?[]? args, CancellationToken cancellationToken = default);

        /// <summary>
        /// 连接加入到group中
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
