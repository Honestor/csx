using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IGroupManager 
    {
        /// <summary>
        /// ���Ӽ��뵽group��
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="groupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
    }
}
