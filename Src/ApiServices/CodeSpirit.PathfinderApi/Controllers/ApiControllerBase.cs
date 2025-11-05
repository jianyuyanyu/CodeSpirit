using CodeSpirit.Shared.Controllers;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CodeSpirit.PathfinderApi.Controllers;

/// <summary>
/// Pathfinder API控制器基类
/// </summary>
[Module("pathfinder", "智能任务", Icon = "fa-solid fa-map-location-dot")]
[Navigation(Icon = "fa-solid fa-map-location-dot", PlatformType = PlatformType.Tenant)]
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/pathfinder/[controller]")]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}
