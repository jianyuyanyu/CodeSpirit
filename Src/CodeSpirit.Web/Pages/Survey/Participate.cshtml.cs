using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CodeSpirit.Core;

namespace CodeSpirit.Web.Pages.Survey;

/// <summary>
/// 参与问卷页面模型
/// </summary>
public class ParticipateModel : PageModel
{
    private readonly ILogger<ParticipateModel> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 问卷访问码
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string AccessCode { get; set; } = string.Empty;

    /// <summary>
    /// 路由中的租户ID参数
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? TenantId { get; set; }

    /// <summary>
    /// 获取有效的租户ID（优先使用路由参数，其次使用当前用户的租户ID）
    /// </summary>
    public string? EffectiveTenantId => !string.IsNullOrEmpty(TenantId) ? TenantId : _currentUser.TenantId;

    /// <summary>
    /// 当前租户名称
    /// </summary>
    public string? TenantName => _currentUser.TenantName;

    /// <summary>
    /// 初始化参与问卷页面模型
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="currentUser">当前用户服务</param>
    public ParticipateModel(ILogger<ParticipateModel> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 页面GET请求处理
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("访问问卷参与页面，访问码: {AccessCode}, 路由租户ID: {RouteTenantId}, 有效租户ID: {EffectiveTenantId}", 
            AccessCode, TenantId, EffectiveTenantId);
    }
}
