using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IHubActivator<THub> where THub : Hub
    {
       
        THub Create();

        void Release(THub hub);
    }
}
