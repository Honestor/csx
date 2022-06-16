using Framework.Core;
using Framework.Core.Dependency;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Uow
{
    public class UnitOfWork : IUnitOfWork, ITransient
    {
        /// <summary>
        /// 工作单元预订Key
        /// </summary>
        public const string UnitOfWorkReservationName = "_ActionUnitOfWork";

        public IUnitOfWorkOptions Options { get; set; }

        private readonly UnitOfWorkDefaultOptions _defaultOptions;

        public bool IsReserved { get; set; }

        public string ReservationName { get; set; }

        public IUnitOfWork Outer { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool IsCompleted { get; private set; }

        public Guid Id { get; set; }

        private bool _isCompleting;

        private Exception _exception;

        public event EventHandler<UnitOfWorkEventArgs> Disposed;

        public event EventHandler<UnitOfWorkFailedEventArgs> Failed;

        public UnitOfWork(IOptions<UnitOfWorkDefaultOptions> options)
        {
            Id = Guid.NewGuid();
            _defaultOptions = options.Value;
            _databaseApis = new ConcurrentDictionary<string, IDatabaseApi>();
            _transactionApis = new ConcurrentDictionary<string, ITransactionApi>();

            System.Diagnostics.Debug.WriteLine("UnitOfWork-------" + Id);
        }

        public virtual void SetOuter(IUnitOfWork outer)
        {
            Outer = outer;
        }

        public virtual void Initialize(UnitOfWorkOptions options)
        {
            Options = _defaultOptions.Normalize(options.Clone());
        }

        /// <summary>
        /// 防止重复提交
        /// </summary>
        private void PreventMultipleComplete()
        {
            if (IsCompleted || _isCompleting)
            {
                throw new FrameworkException("Complete is called before!");
            }
        }

        public virtual void Dispose()
        {
            AsyncHelper.RunSync(
                   () => DisposeAsync()
               );
           
        }

        public async Task DisposeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            await DisposeTransactionsAsync();

            await DisposeConnectionsAsync();

            if (!IsCompleted || _exception != null)
            {
                OnFailed();
            }

            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
            Disposed.InvokeSafely(this, new UnitOfWorkEventArgs(this));
        }

        private async Task DisposeConnectionsAsync()
        {
            foreach (var databaseApi in GetAllActiveDatabaseApis())
            {
                try
                {
                    await databaseApi.DisposeAsync();
                }
                catch(Exception ex)
                {
                    _exception = ex;
                    throw;
                }
            }
        }

        private async Task DisposeTransactionsAsync()
        {
            foreach (var transactionApi in GetAllActiveTransactionApis())
            {
                try
                {
                    await transactionApi.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _exception = ex;
                    throw;
                }
            }
        }

        protected virtual void OnFailed()
        {
            Failed.InvokeSafely(this, new UnitOfWorkFailedEventArgs(this, _exception, _isRolledback));
        }

        #region ORM上下文操作
        private readonly ConcurrentDictionary<string, IDatabaseApi> _databaseApis;
        public IDatabaseApi FindDatabaseApi(string key)
        {
            return _databaseApis.GetOrDefault(key);
        }

        public void AddDatabaseApi(string key, IDatabaseApi api)
        {
            Check.NotNull(key, nameof(key));
            Check.NotNull(api, nameof(api));

            if (_databaseApis.ContainsKey(key))
            {
                throw new FrameworkException("There is already a database API in this unit of work with given key: " + key);
            }
            _databaseApis.TryAdd(key, api);
        }

        /// <summary>
        /// 获取所有数据库操作基本api接口
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IDatabaseApi> GetAllActiveDatabaseApis()
        {
            return _databaseApis.Values.ToImmutableList();
        }

        /// <summary>
        /// 执行所有上下文的SaveChange操作
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var databaseApi in GetAllActiveDatabaseApis())
            {
                //兼容ORM像EF那样支持SaveChange的模块
                if (databaseApi is ISupportsSavingChanges)
                {
                    await (databaseApi as ISupportsSavingChanges).SaveChangesAsync(cancellationToken);
                }
            }
        }
        #endregion

        #region 事务操作
        private readonly ConcurrentDictionary<string, ITransactionApi> _transactionApis;
        public ITransactionApi FindTransactionApi(string key)
        {
            Check.NotNull(key, nameof(key));

            return _transactionApis.GetOrDefault(key);
        }

        public void AddTransactionApi(string key, ITransactionApi api)
        {
            Check.NotNull(key, nameof(key));
            Check.NotNull(api, nameof(api));

            if (_transactionApis.ContainsKey(key))
            {
                throw new FrameworkException("There is already a transaction API in this unit of work with given key: " + key);
            }

            _transactionApis.TryAdd(key, api);
        }

        /// <summary>
        /// 获取所有的事务
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<ITransactionApi> GetAllActiveTransactionApis()
        {
            return _transactionApis.Values.ToImmutableList();
        }

        private bool _isRolledback;
        public virtual async Task CompleteAsync(CancellationToken cancellationToken)
        {
            if (_isRolledback)
            {
                return;
            }
            PreventMultipleComplete();

            try
            {
                _isCompleting = true;
                await CommitTransactionsAsync(cancellationToken);
                IsCompleted = true;
            }
            catch (Exception ex)
            {
                _exception = ex;
                throw;
            }
        }

        /// <summary>
        /// 提交所有的事务
        /// </summary>
        /// <returns></returns>
        protected virtual async Task CommitTransactionsAsync(CancellationToken cancellationToken)
        {
            foreach (var transaction in GetAllActiveTransactionApis())
            {
                try
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                    throw;
                }
            }
        }

        public virtual async Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (_isRolledback)
            {
                return;
            }
            _isRolledback = true;
            await AllRollbackAsync(cancellationToken);
        }

        protected virtual async Task AllRollbackAsync(CancellationToken cancellationToken)
        {
            foreach (var transactionApi in GetAllActiveTransactionApis())
            {
                if (transactionApi is ISupportsRollback)
                {
                    try
                    {
                        await ((ISupportsRollback)transactionApi).RollbackAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _exception = ex;
                        throw;
                    }
                }
            }
        }
        #endregion

        public virtual void Reserve(string reservationName)
        {
            Check.NotNull(reservationName, nameof(reservationName));
            ReservationName = reservationName;
            IsReserved = true;
        }

    }
}
