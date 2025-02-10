using CodeSpirit.Amis;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmisController : ApiControllerBase
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
            JObject amisJson = _amisGenerator.GenerateAmisJsonForController(name);
            return amisJson == null ? NotFound(new { message = $"AMIS JSON for controller '{name}' not found or not supported." }) : Ok(amisJson);
        }

        /// <summary>
        /// 获取 Amis App 的多页应用配置。
        /// </summary>
        /// <returns>配置的 JSON 数据。</returns>
        [HttpGet("site")]
        public async Task<IActionResult> GetSiteConfiguration()
        {
            ApiResponse<Amis.App.AmisApp> site = await _siteConfigurationService.GetSiteConfigurationAsync();
            return Ok(site);
        }
    }
}
