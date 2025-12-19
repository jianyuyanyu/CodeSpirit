using System.ComponentModel;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Dtos.ApiKey;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Navigation.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers;

/// <summary>
/// API密钥管理控制器
/// </summary>
[Authorize]
[DisplayName("API密钥管理")]
[Navigation(Icon = "fa-solid fa-key", TitleResourceKey = "Controller.ApiKeys", TitleResourceType = typeof(NavigationResources))]
public class ApiKeysController : ApiControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ApiKeysController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiKeysController(
        IApiKeyService apiKeyService,
        ICurrentUser currentUser,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// 获取API密钥列表
    /// </summary>
    [HttpGet]
    [DisplayName("获取API密钥列表")]
    public async Task<ActionResult<ApiResponse<PageList<ApiKeyDto>>>> GetApiKeys([FromQuery] ApiKeyQueryDto queryDto)
    {
        if (!_currentUser.Id.HasValue)
        {
            return Unauthorized();
        }

        var apiKeys = await _apiKeyService.GetListAsync(queryDto, _currentUser.Id.Value);
        return SuccessResponse(apiKeys);
    }

    /// <summary>
    /// 获取API密钥详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取API密钥详情")]
    public async Task<ActionResult<ApiResponse<ApiKeyDto>>> GetApiKey(long id)
    {
        if (!_currentUser.Id.HasValue)
        {
            return Unauthorized();
        }

        var apiKey = await _apiKeyService.GetAsync(id, _currentUser.Id.Value);
        return SuccessResponse(apiKey);
    }

    /// <summary>
    /// 创建API密钥
    /// </summary>
    [HttpPost]
    [DisplayName("创建API密钥")]
    public async Task<ActionResult<ApiResponse<CreateApiKeyResultDto>>> CreateApiKey(CreateApiKeyDto createDto)
    {
        if (!_currentUser.Id.HasValue || string.IsNullOrEmpty(_currentUser.TenantId))
        {
            return Unauthorized();
        }

        var result = await _apiKeyService.CreateAsync(createDto, _currentUser.Id.Value, _currentUser.TenantId);
        
        // 返回包含完整密钥信息的响应
        return Ok(new
        {
            status = 0,
            message = $"✅ API密钥创建成功！\n\n📋 密钥名称：{result.Name}\n🔑 完整密钥：{result.ApiKey}\n\n⚠️ 重要提示：密钥只显示一次，请立即复制保存！\n💡 您也可以使用「重新生成」功能重新生成密钥。",
            data = result
        });
    }

    /// <summary>
    /// 更新API密钥
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新API密钥")]
    public async Task<ActionResult<ApiResponse>> UpdateApiKey(long id, UpdateApiKeyDto updateDto)
    {
        if (!_currentUser.Id.HasValue)
        {
            return Unauthorized();
        }

        await _apiKeyService.UpdateAsync(id, updateDto, _currentUser.Id.Value);
        return SuccessResponse();
    }

    /// <summary>
    /// 撤销API密钥
    /// </summary>
    [HttpPut("{id}/revoke")]
    [Operation("撤销", "ajax", null, "确定要撤销此API密钥吗？撤销后将无法使用。", "isActive == true")]
    [DisplayName("撤销API密钥")]
    public async Task<ActionResult<ApiResponse>> RevokeApiKey(long id)
    {
        if (!_currentUser.Id.HasValue)
        {
            return Unauthorized();
        }

        await _apiKeyService.RevokeAsync(id, _currentUser.Id.Value);
        return SuccessResponse();
    }

    /// <summary>
    /// 重新生成API密钥
    /// </summary>
    [HttpPut("{id}/regenerate")]
    [Operation("重新生成", "ajax", null, "⚠️ 重新生成后，旧密钥将立即失效。确定要继续吗？", null,
        FeedbackTitle = "✅ 密钥重新生成成功",
        FeedbackBodyTpl = @"{
            'type': 'form',
            'body': [
                {
                    'type': 'alert',
                    'level': 'warning',
                    'body': '⚠️ 密钥只显示一次，请立即复制并保存！关闭后将无法再次查看。',
                    'className': 'mb-3',
                    'showIcon': true
                },
                {
                    'type': 'tpl',
                    'tpl': '<div class=""mb-3""><label class=""control-label"">密钥名称</label><div class=""form-control-static"">${name}</div></div>'
                },
                {
                    'type': 'tpl',
                    'tpl': '<div class=""mb-3""><label class=""control-label"">完整密钥</label><div class=""form-control-static"" style=""word-break: break-all; font-family: monospace; background: #f5f5f5; padding: 8px; border-radius: 4px;"">${apiKey}</div></div>'
                },
                {
                    'type': 'button',
                    'label': '复制密钥',
                    'icon': 'fa fa-copy',
                    'level': 'primary',
                    'actionType': 'copy',
                    'content': '${apiKey}'
                }
            ]
        }",
        Icon = "fa-solid fa-rotate")]
    [DisplayName("重新生成密钥")]
    public async Task<ActionResult<ApiResponse<CreateApiKeyResultDto>>> RegenerateApiKey(long id)
    {
        if (!_currentUser.Id.HasValue || string.IsNullOrEmpty(_currentUser.TenantId))
        {
            return Unauthorized();
        }

        var result = await _apiKeyService.RegenerateAsync(id, _currentUser.Id.Value, _currentUser.TenantId);
        return Ok(new
        {
            message = "API密钥重新生成成功！",
            data = result,
        });
    }

    /// <summary>
    /// 删除API密钥
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除API密钥")]
    public async Task<ActionResult<ApiResponse>> DeleteApiKey(long id)
    {
        if (!_currentUser.Id.HasValue)
        {
            return Unauthorized();
        }

        await _apiKeyService.DeleteAsync(id, _currentUser.Id.Value);
        return SuccessResponse();
    }
}

