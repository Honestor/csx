using Framework.Core;
using Framework.Core.Data;
using Framework.Core.Dependency;
using Framework.Uow;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Framework.Dapper.Uow
{
    public class DapperUnitOfWorkDbProvider : IDbProvider, ITransient
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IDbConnectionProvider _dbConnectionProvider;
        public DapperUnitOfWorkDbProvider(
            IUnitOfWorkManager unitOfWorkManager,
            IDbConnectionProvider dbConnectionProvider)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _dbConnectionProvider = dbConnectionProvider;
        }

        public async Task<DbConnection> GetConnectionAsync()
        {
            var unitOfWork = _unitOfWorkManager.Current;
            if (unitOfWork == null)
            {
                throw new FrameworkException("A connection can only be created inside a unit of work!");
            }
            var databaseApi = (DapperDatabaseApi)unitOfWork.FindDatabaseApi(unitOfWork.Id.ToString());

            if (databaseApi == null)
            {
                var connection = await _dbConnectionProvider.GetAsync();
                databaseApi = new DapperDatabaseApi(connection);
                unitOfWork.AddDatabaseApi(unitOfWork.Id.ToString(), databaseApi);
            }
            return databaseApi.DbConnection;
        }


        public async Task<IDbTransaction> GetTransactionAsync()
        {
            var unitOfWork = _unitOfWorkManager.Current;
            if (unitOfWork == null)
            {
                throw new FrameworkException("A connection can only be created inside a unit of work!");
            }
            var transactionApi = (DapperTransactionApi)unitOfWork.FindTransactionApi(unitOfWork.Id.ToString());
            var databaseApi = (DapperDatabaseApi)unitOfWork.FindDatabaseApi(unitOfWork.Id.ToString());
            if (databaseApi == null)
            {
                throw new FrameworkException("A transaction can only be created after connection!");
            }
            else
            {
                var connection = databaseApi.DbConnection;
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
            }
            if (transactionApi == null)
            {
                var connection = databaseApi.DbConnection;
                var options = unitOfWork.Options ?? new UnitOfWorkOptions();
                var dbtransaction = options.IsolationLevel.HasValue
                    ? await connection.BeginTransactionAsync(options.IsolationLevel.Value)
                    : await connection.BeginTransactionAsync();
                transactionApi = new DapperTransactionApi(dbtransaction);
                unitOfWork.AddTransactionApi(unitOfWork.Id.ToString(), transactionApi);
            }
            return transactionApi.DbTransaction;
        }
    }
}
