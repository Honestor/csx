using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class HubMetadata
    {
        public HubMetadata(Type hubType)
        {
            HubType = hubType;
        }

        /// <summary>
        /// Hub¿‡–Õ
        /// </summary>
        public Type HubType { get; }
    }
}
