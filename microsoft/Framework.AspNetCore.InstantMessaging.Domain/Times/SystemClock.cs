using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public long UtcNowTicks => DateTimeOffset.UtcNow.Ticks;

        public DateTimeOffset UtcNowUnsynchronized => DateTimeOffset.UtcNow;
    }
}
