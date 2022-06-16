using Framework.Core.Data;
using Framework.Core.Dependency;
using MySqlConnector;
using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Data.MySql
{
    public class MySqlDbConnectionProvider : IDbConnectionProvider, ITransient
    {
        private IConnectionStringProvider _connectionStringProvider;
        public MySqlDbConnectionProvider(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }

        public async Task<DbConnection> GetAsync()
        {
            var connection= new MySqlConnection(_connectionStringProvider.GetConnectionString());
            await connection.OpenAsync();
            return connection;
        }
    }
}
