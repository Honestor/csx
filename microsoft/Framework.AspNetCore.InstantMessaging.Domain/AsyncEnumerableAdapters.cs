using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal static class AsyncEnumerableAdapters
    {
        public static IAsyncEnumerable<object> MakeCancelableAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
        {
            return new CancelableAsyncEnumerable<T>(asyncEnumerable, cancellationToken);
        }

        public static IAsyncEnumerable<T> MakeCancelableTypedAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationTokenSource cts)
        {
            return new CancelableTypedAsyncEnumerable<T>(asyncEnumerable, cts);
        }

        public static async IAsyncEnumerable<object> MakeAsyncEnumerableFromChannel<T>(ChannelReader<T> channel, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in channel.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }

        private class CancelableTypedAsyncEnumerable<TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<TResult> _asyncEnumerable;
            private readonly CancellationTokenSource _cts;

            public CancelableTypedAsyncEnumerable(IAsyncEnumerable<TResult> asyncEnumerable, CancellationTokenSource cts)
            {
                _asyncEnumerable = asyncEnumerable;
                _cts = cts;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                var enumerator = _asyncEnumerable.GetAsyncEnumerator(_cts.Token);
                if (cancellationToken.CanBeCanceled)
                {
                    var registration = cancellationToken.Register((ctsState) =>
                    {
                        ((CancellationTokenSource)ctsState).Cancel();
                    }, _cts);

                    return new CancelableEnumerator<TResult>(enumerator, registration);
                }

                return enumerator;
            }

            private class CancelableEnumerator<T> : IAsyncEnumerator<T>
            {
                private IAsyncEnumerator<T> _asyncEnumerator;
                private readonly CancellationTokenRegistration _cancellationTokenRegistration;

                public T Current => (T)_asyncEnumerator.Current;

                public CancelableEnumerator(IAsyncEnumerator<T> asyncEnumerator, CancellationTokenRegistration registration)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _cancellationTokenRegistration = registration;
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    return _asyncEnumerator.MoveNextAsync();
                }

                public ValueTask DisposeAsync()
                {
                    _cancellationTokenRegistration.Dispose();
                    return _asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>Converts an IAsyncEnumerable of T to an IAsyncEnumerable of object.</summary>
        private class CancelableAsyncEnumerable<T> : IAsyncEnumerable<object>
        {
            private readonly IAsyncEnumerable<T> _asyncEnumerable;
            private readonly CancellationToken _cancellationToken;

            public CancelableAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken)
            {
                _asyncEnumerable = asyncEnumerable;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                // Assume that this will be iterated through with await foreach which always passes a default token.
                // Instead use the token from the ctor.
                Debug.Assert(cancellationToken == default);

                var enumeratorOfT = _asyncEnumerable.GetAsyncEnumerator(_cancellationToken);
                return enumeratorOfT as IAsyncEnumerator<object> ?? new BoxedAsyncEnumerator(enumeratorOfT);
            }

            private class BoxedAsyncEnumerator : IAsyncEnumerator<object>
            {
                private IAsyncEnumerator<T> _asyncEnumerator;

                public BoxedAsyncEnumerator(IAsyncEnumerator<T> asyncEnumerator)
                {
                    _asyncEnumerator = asyncEnumerator;
                }

                public object Current => _asyncEnumerator.Current;

                public ValueTask<bool> MoveNextAsync()
                {
                    return _asyncEnumerator.MoveNextAsync();
                }

                public ValueTask DisposeAsync()
                {
                    return _asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
}
