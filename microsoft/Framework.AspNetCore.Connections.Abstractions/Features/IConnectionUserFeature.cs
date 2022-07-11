using System.Security.Claims;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public interface IConnectionUserFeature
    {
        ClaimsPrincipal? User { get; set; }
    }
}
