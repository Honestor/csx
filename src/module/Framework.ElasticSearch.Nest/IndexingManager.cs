using System.Threading.Tasks;

namespace Framework.ElasticSearch.Nest
{
    /// <summary>
    /// 索引管理
    /// </summary>
    public class IndexingManager<T> where T:class,new()
    {
        private IClientProvider _clientProvider;

        public IndexingManager(IClientProvider clientProvider)
        {
            _clientProvider = clientProvider;
        }

        public async Task<bool> AddAsync(T entity,string? indexName =null)
        {
            var client = await _clientProvider.GetClientAsync();
            var response = await client.IndexAsync(entity, i => i.Index(indexName??nameof(T)));
            return response.IsValid;
        }
    }
}
