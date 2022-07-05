using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.IdentityServer4.SSO.MVC
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [Authorize]
        public async Task<string> Get()
        {
            return "111";
        }
    }
}
