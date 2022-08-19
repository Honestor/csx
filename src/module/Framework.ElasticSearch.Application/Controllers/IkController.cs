using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Framework.Core;
using System.Text;

namespace Framework.ElasticSearch.Ik.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class IkController : ControllerBase
    {
        private IHttpContextAccessor _httpContextAccessor;
        public IkController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// dll嵌入文件方式扩展分词文件 注意不要使用此方法,做记录用
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet("extdicsdll")]
        public async Task GetExtDicsDllAsync(string type)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var fileStream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().FirstOrDefault(f => f.Contains(type))))
            {
                byte[] result = new byte[fileStream.Length];
                fileStream.Read(result, 0, result.Length);
                fileStream.Seek(0, SeekOrigin.Begin);
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Response.Headers["Last-Modified"] = result.Length.ToString();
                    _httpContextAccessor.HttpContext.Response.Headers["ETag"] = result.Length.ToString();
                    _httpContextAccessor.HttpContext.Response.ContentType = "text/plain;charset=utf-8";
                    await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(result, _httpContextAccessor.HttpContext.RequestAborted);
                }
            }  
        }

        [HttpGet("extdics")]
        [HttpHead("extdics")]
        public async Task GetExtDicsAsync(string type)
        {
            var filePath = type == "stop" ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TokenizerExtFiles", "remote_ext_stopwords.txt") : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TokenizerExtFiles", "remote_ext_dict.txt");
            using (var fileStream =System.IO.File.Open(filePath, FileMode.Open))
            {

                byte[] result = new byte[fileStream.Length];
                await fileStream.ReadAsync(result, 0, (int)fileStream.Length, _httpContextAccessor.HttpContext.RequestAborted);
                var contentWithNoBom= Encoding.UTF8.GetString(result).WithNobom();
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Response.Headers["Last-Modified"] = result.Length.ToString();
                    _httpContextAccessor.HttpContext.Response.Headers["ETag"] = result.Length.ToString();
                    _httpContextAccessor.HttpContext.Response.ContentType = "text/plain;charset=utf-8";
                    await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(contentWithNoBom), _httpContextAccessor.HttpContext.RequestAborted);
                }
            }
        }
    }
}
