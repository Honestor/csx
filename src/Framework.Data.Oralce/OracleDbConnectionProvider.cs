using Framework.Core.Data;
using Framework.Core.Dependency;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Data.Oralce
{
    public class OracleDbConnectionProvider : IDbConnectionProvider, ITransient
    {
        private IConnectionStringProvider _connectionStringProvider;
        public OracleDbConnectionProvider(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }


        public async Task<DbConnection> GetAsync()
        {
            var connection = new OracleConnection(_connectionStringProvider.GetConnectionString());
            await connection.OpenAsync();
            return connection;
        }
    }
}
