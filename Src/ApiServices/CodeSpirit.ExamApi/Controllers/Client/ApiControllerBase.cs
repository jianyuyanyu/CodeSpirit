using CodeSpirit.Core.Attributes;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers.Client
{
    /// <summary>
    /// 考试系统客户端API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Module("default", displayName: "默认")]
    public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
    {
    }
}
