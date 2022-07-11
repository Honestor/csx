using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub连接仓储
    /// </summary>
    public class HubConnectionStore
    {
        private readonly ConcurrentDictionary<string, HubConnectionContext> _connections =
            new ConcurrentDictionary<string, HubConnectionContext>(StringComparer.Ordinal);

        public void Add(HubConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        #region 迭代器实现
        public HubConnectionContext? this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public readonly struct Enumerator : IEnumerator<HubConnectionContext>
        {
            private readonly IEnumerator<KeyValuePair<string, HubConnectionContext>> _enumerator;

            public Enumerator(HubConnectionStore hubConnectionList)
            {
                _enumerator = hubConnectionList._connections.GetEnumerator();
            }

            public HubConnectionContext Current => _enumerator.Current.Value;

            object IEnumerator.Current => Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        } 
        #endregion
    }
}
