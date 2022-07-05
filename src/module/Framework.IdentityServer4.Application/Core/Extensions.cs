using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;

namespace Framework.IdentityServer4.Application
{
    public static class Extensions
    {
        /// <summary>
        /// 跳转url是本地的还是远程的
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
               && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";
            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }
    }
}
