using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }

        long UtcNowTicks { get; }

        DateTimeOffset UtcNowUnsynchronized { get; }
    }
}
