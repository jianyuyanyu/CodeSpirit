using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Resources;
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
[Module("Survey", "问卷调查", DisplayNameResourceKey = "Module.Survey", DisplayNameResourceType = typeof(NavigationResources), Icon = "fa-solid fa-poll")]
[Navigation(Icon = "fa-solid fa-poll", PlatformType = PlatformType.Tenant, TitleResourceKey = "Module.Survey", TitleResourceType = typeof(NavigationResources))]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}
