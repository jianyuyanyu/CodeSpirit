using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ConfigCenter.Controllers
{
    /// <summary>
    /// 配置中心API控制器基类
    /// </summary>
    [ApiController]
    //[Authorize(policy: "DynamicPermissions")]
    [Route("api/config/[controller]")]
    [Module("config", "配置中心", Icon = "fa-solid fa-sliders")]
    [Navigation(Icon = "fa-solid fa-sliders", PlatformType = PlatformType.System)]
    [Platform(PlatformType.System)]
    public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
}
