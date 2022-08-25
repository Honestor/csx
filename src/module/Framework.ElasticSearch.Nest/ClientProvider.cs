using Elasticsearch.Net;
using Framework.Core.Dependency;
using Framework.ElasticSearch.Nest.Configurations;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.ElasticSearch.Nest
{
    public class ClientProvider : IClientProvider, ISingleton
    {
        private NestOptions _nestOptions;

        public ClientProvider(IOptionsMonitor<NestOptions> nestOptions)
        {
            _nestOptions = nestOptions.CurrentValue;
        }

        public async Task<ElasticClient> GetClientAsync()
        {
            if (_nestOptions.Uris.Count == 0)
                throw new Exception("please set es server connected ip adress");
            var connectionPool = new SniffingConnectionPool(_nestOptions.Uris.Select(s => new Uri(s)));
            var settings = new ConnectionSettings(connectionPool);
            var client = new ElasticClient(settings);
            await Task.CompletedTask;
            return client;
        }
    }
}
