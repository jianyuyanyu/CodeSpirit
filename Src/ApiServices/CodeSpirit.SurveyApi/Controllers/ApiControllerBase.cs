using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷系统API控制器基类
/// </summary>
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/survey/[controller]")]
[Module("Survey", "问卷调查", Icon = "fa-solid fa-poll")]
[Navigation(Icon = "fa-solid fa-poll", PlatformType = PlatformType.Tenant)]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}
