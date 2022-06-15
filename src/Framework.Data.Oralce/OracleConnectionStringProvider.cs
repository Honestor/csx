using Framework.Core;
using Framework.Core.Data;
using Framework.Core.Dependency;

namespace Framework.Data.Oralce
{
    public class OracleConnectionStringProvider : IConnectionStringProvider,ITransient
    {
        private OracleDbOptionsProvider _oracleDbOptionsProvider;

        public OracleConnectionStringProvider(OracleDbOptionsProvider oracleDbOptionsProvider)
        {
            _oracleDbOptionsProvider = oracleDbOptionsProvider;
        }

        public string GetConnectionName()
        {
            var connectionName = _oracleDbOptionsProvider.Get()?.ConnectionName;
            if (string.IsNullOrEmpty(connectionName))
            {
                throw new FrameworkException("OracleDbOptions configuration node not found ConnectionName in appseeting.json ,please set it");
            }
            return connectionName;
        }

        public string GetConnectionString()
        {
            var connectionString = _oracleDbOptionsProvider.Get()?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new FrameworkException("OracleDbOptions configuration node not found ConnectionString in appseeting.json ,please set it");
            }
            return connectionString;
        }
    }
}