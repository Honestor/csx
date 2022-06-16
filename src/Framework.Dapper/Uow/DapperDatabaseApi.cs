using Framework.Uow;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Dapper.Uow
{
    public class DapperDatabaseApi : IDatabaseApi
    {
        public DbConnection DbConnection { get; }

        public DapperDatabaseApi(DbConnection _dbConnection)
        {
            DbConnection = _dbConnection;
        }

        public async ValueTask DisposeAsync()
        {
            await DbConnection.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
