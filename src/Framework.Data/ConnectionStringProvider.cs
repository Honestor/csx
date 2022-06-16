using Framework.Core;
using Framework.Core.Dependency;
using Microsoft.Extensions.Options;

namespace Framework.Core.Data
{
    public class ConnectionStringProvider : IConnectionStringProvider,ITransient
    {
        private DbOptions _dbOptions;

        public ConnectionStringProvider(IOptionsMonitor<DbOptions> options)
        {
            _dbOptions = options.CurrentValue;
        }

        public string GetConnectionName()
        {
            var connectionName = _dbOptions?.ConnectionName;
            if (string.IsNullOrEmpty(connectionName))
            {
                throw new FrameworkException("DbOptions configuration node not found ConnectionName in appseeting.json ,please set it");
            }
            return connectionName;
        }

        public string GetConnectionString()
        {
            var connectionString = _dbOptions?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new FrameworkException("DbOptions configuration node not found ConnectionString in appseeting.json ,please set it");
            }
            return connectionString;
        }
    }
}