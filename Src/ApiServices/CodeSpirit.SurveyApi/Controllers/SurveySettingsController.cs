using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Settings;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷系统设置控制器
/// </summary>
[DisplayName("系统设置")]
[Navigation(Icon = "fa-solid fa-cog", PlatformType = PlatformType.Tenant)]
public class SurveySettingsController : ApiControllerBase
{
    private readonly ISurveySettingsService _settingsService;
    private readonly ILogger<SurveySettingsController> _logger;

    /// <summary>
    /// 初始化设置控制器
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveySettingsController(
        ISurveySettingsService settingsService,
        ILogger<SurveySettingsController> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取问卷系统设置
    /// </summary>
    /// <returns>设置信息</returns>
    [HttpGet]
    [DisplayName("获取系统设置")]
    public async Task<ActionResult<ApiResponse<SurveySettingsDto>>> GetSurveySettings()
    {
        var settings = await _settingsService.GetSurveySettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 更新问卷系统设置
    /// </summary>
    /// <param name="settings">设置信息</param>
    /// <returns>操作结果</returns>
    [HttpPut]
    [DisplayName("更新系统设置")]
    public async Task<ActionResult<ApiResponse>> UpdateSurveySettings([FromBody] SurveySettingsDto settings)
    {
        await _settingsService.UpdateSurveySettingsAsync(settings);
        return SuccessResponse("设置更新成功");
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpPost("reset")]
    [Operation("重置默认", "ajax", null, "确定要重置为默认设置吗？")]
    [DisplayName("重置设置")]
    public async Task<ActionResult<ApiResponse>> ResetToDefaultSettings()
    {
        await _settingsService.ResetToDefaultSettingsAsync();
        return SuccessResponse("设置已重置为默认值");
    }

    /// <summary>
    /// 导出设置
    /// </summary>
    /// <returns>设置JSON</returns>
    [HttpGet("export")]
    [DisplayName("导出设置")]
    public async Task<ActionResult<ApiResponse<SurveySettingsDto>>> ExportSettings()
    {
        var settings = await _settingsService.GetSurveySettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 导入设置
    /// </summary>
    /// <param name="settings">设置信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("import")]
    [Operation("导入设置", "form")]
    [DisplayName("导入设置")]
    public async Task<ActionResult<ApiResponse>> ImportSettings([FromBody] SurveySettingsDto settings)
    {
        await _settingsService.UpdateSurveySettingsAsync(settings);
        return SuccessResponse("设置导入成功");
    }

    /// <summary>
    /// 获取自动保存设置
    /// </summary>
    /// <returns>自动保存设置</returns>
    [HttpGet("auto-save")]
    [DisplayName("获取自动保存设置")]
    public async Task<ActionResult<ApiResponse<AutoSaveSettings>>> GetAutoSaveSettings()
    {
        var settings = await _settingsService.GetAutoSaveSettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    /// <returns>LLM设置</returns>
    [HttpGet("llm")]
    [DisplayName("获取LLM设置")]
    public async Task<ActionResult<ApiResponse<LLMSettings>>> GetLLMSettings()
    {
        var settings = await _settingsService.GetLLMSettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 获取默认限制设置
    /// </summary>
    /// <returns>默认限制设置</returns>
    [HttpGet("restrictions")]
    [DisplayName("获取限制设置")]
    public async Task<ActionResult<ApiResponse<DefaultRestrictionsSettings>>> GetDefaultRestrictionsSettings()
    {
        var settings = await _settingsService.GetDefaultRestrictionsSettingsAsync();
        return SuccessResponse(settings);
    }
}
