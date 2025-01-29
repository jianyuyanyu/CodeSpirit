using CodeSpirit.IdentityApi.Amis;
using CodeSpirit.IdentityApi.Amis.App;
using CodeSpirit.IdentityApi.Amis.Attributes;
using CodeSpirit.IdentityApi.Amis.Configuration;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmisController : ControllerBase
    {
        private readonly AmisGenerator _amisGenerator;
        private readonly ISiteConfigurationService _siteConfigurationService;

        public AmisController(AmisGenerator amisGenerator, ISiteConfigurationService siteConfigurationService)
        {
            _amisGenerator = amisGenerator;
            _siteConfigurationService = siteConfigurationService;
        }

        /// <summary>
        /// 生成指定控制器的 AMIS JSON
        /// 路由示例：/api/amis/users
        /// </summary>
        /// <param name="name">控制器名称，不含 "Controller" 后缀</param>
        /// <returns>AMIS JSON 配置</returns>
        [HttpGet("{name}")]
        public IActionResult GenerateAmis([FromRoute] string name)
        {
            var amisJson = _amisGenerator.GenerateAmisJsonForController(name);
            if (amisJson == null)
            {
                return NotFound(new { message = $"AMIS JSON for controller '{name}' not found or not supported." });
            }

            return Ok(amisJson);
        }

        /// <summary>
        /// 获取 Amis App 的多页应用配置。
        /// </summary>
        /// <returns>配置的 JSON 数据。</returns>
        [HttpGet("site")]
        public IActionResult GetSiteConfiguration()
        {
            var site = _siteConfigurationService.GetSiteConfiguration();
            return Ok(site);
        }
    }
}
