using CodeSpirit.IdentityApi.Amis;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmisController : ControllerBase
    {
        private readonly AmisGenerator _amisGenerator;

        public AmisController(AmisGenerator amisGenerator)
        {
            _amisGenerator = amisGenerator;
        }

        /// <summary>
        /// 生成指定控制器的 AMIS JSON
        /// 路由示例：/api/amis/users
        /// </summary>
        /// <param name="name">控制器名称，不含 "Controller" 后缀</param>
        /// <returns>AMIS JSON 配置</returns>
        [HttpGet("{name}")]
        public IActionResult GenerateAmis([FromRoute]string name)
        {
            var amisJson = _amisGenerator.GenerateAmisJsonForController(name);
            if (amisJson == null)
            {
                return NotFound(new { message = $"AMIS JSON for controller '{name}' not found or not supported." });
            }

            return Ok(amisJson);
        }
    }
}
