namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IUserIdProvider
    {
        string? GetUserId(HubConnectionContext connection);
    }
}
