using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Resources;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 身份认证API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Route("api/identity/[controller]")]
    [Module("identity", displayName: "用户中心", DisplayNameResourceKey = "Module.Identity", DisplayNameResourceType = typeof(NavigationResources), Icon = "fa-solid fa-user-group")]
    [Navigation(Icon = "fa-solid fa-user-group", PlatformType = PlatformType.Both)]
    public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
}
