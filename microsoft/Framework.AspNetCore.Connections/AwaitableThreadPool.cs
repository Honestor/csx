using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections
{
    internal static class AwaitableThreadPool
    {
        public static Awaitable Yield()
        {
            return new Awaitable();
        }

        public readonly struct Awaitable : ICriticalNotifyCompletion
        {
            public void GetResult()
            {

            }

            public Awaitable GetAwaiter() => this;

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                Task.Run(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }
        }
    }
}
