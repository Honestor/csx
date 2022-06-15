using Framework.Core.Data;
using Framework.Core.Dependency;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Framework.Data.Oralce
{
    public class OracleDbConnectionProvider : IDbConnectionProvider, ITransient
    {
        private IConnectionStringProvider _connectionStringProvider;
        public OracleDbConnectionProvider(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }

        public DbConnection Get()
        {
            return new OracleConnection(_connectionStringProvider.GetConnectionString());
        }
    }
}
