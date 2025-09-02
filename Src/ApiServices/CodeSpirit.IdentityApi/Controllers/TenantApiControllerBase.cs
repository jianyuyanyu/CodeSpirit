using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 租户管理API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Route("api/identity/[controller]")]
    [Module("tenant", displayName: "租户管理", Icon = "fa-solid fa-building")]
    [Navigation(Icon = "fa-solid fa-building", PlatformType = PlatformType.System)]
    public abstract class TenantApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
}
