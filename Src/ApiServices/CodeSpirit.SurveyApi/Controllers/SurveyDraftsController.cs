using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Draft;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷草稿管理控制器
/// </summary>
[DisplayName("草稿管理")]
[Navigation(Icon = "fa-solid fa-file-text", PlatformType = PlatformType.Tenant)]
public class SurveyDraftsController : ApiControllerBase
{
    private readonly ISurveyDraftService _draftService;
    private readonly ILogger<SurveyDraftsController> _logger;

    /// <summary>
    /// 初始化草稿管理控制器
    /// </summary>
    /// <param name="draftService">草稿服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveyDraftsController(
        ISurveyDraftService draftService,
        ILogger<SurveyDraftsController> logger)
    {
        ArgumentNullException.ThrowIfNull(draftService);
        ArgumentNullException.ThrowIfNull(logger);

        _draftService = draftService;
        _logger = logger;
    }

    /// <summary>
    /// 自动保存草稿
    /// </summary>
    /// <param name="request">保存请求</param>
    /// <returns>保存结果</returns>
    [HttpPost("auto-save")]
    [DisplayName("自动保存草稿")]
    public async Task<ActionResult<ApiResponse<SurveyDraftSaveResult>>> AutoSaveDraft([FromBody] AutoSaveDraftRequest request)
    {
        // 从请求头中获取IP地址
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.UserAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _draftService.AutoSaveDraftAsync(request);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>草稿数据</returns>
    [HttpGet]
    [DisplayName("获取草稿")]
    public async Task<ActionResult<ApiResponse<SurveyDraftDto?>>> GetDraft(
        [FromQuery] int surveyId,
        [FromQuery] string sessionId,
        [FromQuery] string? userId = null)
    {
        var draft = await _draftService.GetDraftAsync(surveyId, sessionId, userId);
        return Ok(new ApiResponse<SurveyDraftDto?>(0, "操作成功！", draft));
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>操作结果</returns>
    [HttpDelete]
    [DisplayName("删除草稿")]
    public async Task<ActionResult<ApiResponse>> DeleteDraft(
        [FromQuery] int surveyId,
        [FromQuery] string sessionId,
        [FromQuery] string? userId = null)
    {
        await _draftService.DeleteDraftAsync(surveyId, sessionId, userId);
        return SuccessResponse("草稿删除成功");
    }

    /// <summary>
    /// 获取用户的所有草稿
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>草稿列表</returns>
    [HttpGet("user/{userId}")]
    [DisplayName("获取用户草稿")]
    public async Task<ActionResult<ApiResponse<List<SurveyDraftDto>>>> GetUserDrafts(string userId)
    {
        var drafts = await _draftService.GetUserDraftsAsync(userId);
        return SuccessResponse(drafts);
    }

    /// <summary>
    /// 获取会话的所有草稿
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>草稿列表</returns>
    [HttpGet("session/{sessionId}")]
    [DisplayName("获取会话草稿")]
    public async Task<ActionResult<ApiResponse<List<SurveyDraftDto>>>> GetSessionDrafts(string sessionId)
    {
        var drafts = await _draftService.GetSessionDraftsAsync(sessionId);
        return SuccessResponse(drafts);
    }

    /// <summary>
    /// 清理过期草稿
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup")]
    [Operation("清理过期草稿", "ajax", null, "确定要清理过期草稿吗？")]
    [DisplayName("清理过期草稿")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupExpiredDrafts()
    {
        var count = await _draftService.CleanupExpiredDraftsAsync();
        var result = new { CleanedCount = count, Message = $"已清理 {count} 个过期草稿" };
        return Ok(new ApiResponse<object>(0, "操作成功！", result));
    }
}
