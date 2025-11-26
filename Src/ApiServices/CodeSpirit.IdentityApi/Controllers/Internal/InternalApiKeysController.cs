using CodeSpirit.Audit.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Dtos.ApiKey;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Internal;

/// <summary>
/// 内部API密钥验证控制器
/// </summary>
[ApiController]
[Route("internal/api-keys")]
[ApiExplorerSettings(IgnoreApi = true)] // 不在Swagger中显示
[DisplayName("内部API密钥验证")]
[NoAudit()]
[Module("default")]
public class InternalApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<InternalApiKeysController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public InternalApiKeysController(IApiKeyService apiKeyService, ILogger<InternalApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// 验证API密钥
    /// </summary>
    /// <param name="apiKey">明文API密钥</param>
    /// <returns>如果验证成功，返回ApiKeyValidationDto信息；否则返回401</returns>
    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ApiKeyValidationDto>>> ValidateApiKey([FromQuery] string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("ValidateApiKey: API密钥为空。");
            return Unauthorized();
        }

        var validatedKey = await _apiKeyService.ValidateAsync(apiKey);

        if (validatedKey == null)
        {
            _logger.LogWarning("ValidateApiKey: API密钥验证失败或已过期。");
            return Unauthorized();
        }

        _logger.LogInformation("ValidateApiKey: API密钥验证成功，用户ID: {UserId}, 用户名: {UserName}, 租户ID: {TenantId}, 租户名称: {TenantName}",
            validatedKey.UserId, validatedKey.User?.UserName, validatedKey.TenantId, validatedKey.TenantName);

        return Ok(new ApiResponse<ApiKeyValidationDto>(0, "验证成功", validatedKey));
    }
}

