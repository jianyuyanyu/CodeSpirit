using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.FileStorageApi.Controllers;

/// <summary>
/// 文件存储API控制器基类
/// </summary>
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/file/[controller]")]
[Module("file", displayName: "文件存储", Icon = "fa-solid fa-folder")]
[Navigation(Icon = "fa-solid fa-folder", PlatformType = PlatformType.Both)]
public abstract class ApiControllerBase : CodeSpirit.Shared.Controllers.ApiControllerBase
{
}
