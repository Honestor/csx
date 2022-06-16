using Framework.Uow;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Dapper.Uow
{
    public class DapperTransactionApi : ITransactionApi, ISupportsRollback
    {
        public DbTransaction DbTransaction { get; }

        public DapperTransactionApi(DbTransaction dbTransaction)
        {
            DbTransaction = dbTransaction;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return DbTransaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return DbTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DbTransaction.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
