using CodeSpirit.Core.Attributes;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.MessagingApi.Controllers;

/// <summary>
/// 消息中心API控制器基类
/// </summary>
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/messaging/[controller]")]
[Module("messaging", displayName: "消息中心", Icon = "fa-solid fa-envelope")]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}