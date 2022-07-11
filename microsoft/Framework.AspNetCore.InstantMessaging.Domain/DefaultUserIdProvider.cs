using System.Security.Claims;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class DefaultUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
