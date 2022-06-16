using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Core.Data
{
    /// <summary>
    /// 数据库提供者
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// 获取数据库连接字符串 异步
        /// </summary>
        /// <returns></returns>
        Task<DbConnection> GetConnectionAsync();

        /// <summary>
        /// 获取事务 异步
        /// </summary>
        /// <returns></returns>
        Task<IDbTransaction> GetTransactionAsync();
    }
}
