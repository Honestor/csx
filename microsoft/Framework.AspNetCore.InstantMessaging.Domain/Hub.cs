using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public abstract class Hub : IDisposable
    {
        private IHubCallerClients _clients = default!;

        /// <summary>
        /// 客户端连接Caller
        /// </summary>
        public IHubCallerClients Clients
        {
            get
            {
                CheckDisposed();
                return _clients;
            }
            set
            {
                CheckDisposed();
                _clients = value;
            }
        }

        private HubCallerContext _context = default!;
        public HubCallerContext Context
        {
            get
            {
                CheckDisposed();
                return _context;
            }
            set
            {
                CheckDisposed();
                _context = value;
            }
        }

        private IGroupManager _groups = default!;
        public IGroupManager Groups
        {
            get
            {
                CheckDisposed();
                return _groups;
            }
            set
            {
                CheckDisposed();
                _groups = value;
            }
        }

        public virtual Task OnConnectedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDisconnectedAsync(Exception? exception)
        {
            return Task.CompletedTask;
        }

        #region 资源释放
        private bool _disposed;
        protected virtual void Dispose(bool disposing) { }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Dispose(true);

            _disposed = true;
        }
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
        #endregion
    }
}
