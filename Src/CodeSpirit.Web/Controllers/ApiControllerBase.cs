using CodeSpirit.Core.Attributes;
using CodeSpirit.Shared.Controllers;
using CodeSpirit.Web.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// Web项目API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Route("api/web/[controller]")]
    [Module(Constants.Constants.Module, "系统管理", Icon = "fa-solid fa-gear")]
    public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
} 