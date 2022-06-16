using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Core.Data
{
    /// <summary>
    /// 数据库连接字符串Provider
    /// </summary>
    public interface IDbConnectionProvider
    {
        /// <summary>
        /// 同步获取数据库连接
        /// </summary>
        /// <returns></returns>
        Task<DbConnection> GetAsync();
    }
}
