using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 审批模块API控制器基类
/// </summary>
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/approval/[controller]")]
[Module("approval", "智能审批", Icon = "fa-solid fa-check-circle")]
[Navigation(Icon = "fa-solid fa-check-circle", PlatformType = PlatformType.Tenant)]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}
