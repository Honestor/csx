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
        /// Hub����
        /// </summary>
        public Type HubType { get; }
    }
}
