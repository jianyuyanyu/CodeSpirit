using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CodeSpirit.Core;

namespace CodeSpirit.Web.Pages.Survey;

/// <summary>
/// 问卷提交成功页面模型
/// </summary>
public class SuccessModel : PageModel
{
    private readonly ILogger<SuccessModel> _logger;
    private readonly ICurrentUser _currentUser;

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
    /// 初始化问卷提交成功页面模型
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="currentUser">当前用户服务</param>
    public SuccessModel(ILogger<SuccessModel> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 页面GET请求处理
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("访问问卷提交成功页面，路由租户ID: {RouteTenantId}, 有效租户ID: {EffectiveTenantId}", 
            TenantId, EffectiveTenantId);
    }
}
