using Nest;
using System.Threading.Tasks;

namespace Framework.ElasticSearch.Nest
{
    public interface IClientProvider
    {
        /// <summary>
        /// 获取客户端
        /// </summary>
        /// <returns></returns>
        Task<ElasticClient> GetClientAsync();
    }
}
