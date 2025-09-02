using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.ExamApi.Constants;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers
{
    /// <summary>
    /// 考试系统API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Route("api/exam/[controller]")]
    [Module(ExamConstants.ExamModule, "考试中心", Icon = "fa-solid fa-graduation-cap")]
    [Navigation(Icon = "fa-solid fa-graduation-cap", PlatformType = PlatformType.Tenant)]
    public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
}
