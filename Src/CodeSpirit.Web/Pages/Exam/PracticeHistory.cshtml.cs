using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Web.Pages.Exam;

/// <summary>
/// 我的练习历史页面模型
/// </summary>
public class PracticeHistoryModel : PageModel
{
    private readonly ILogger<PracticeHistoryModel> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public PracticeHistoryModel(ILogger<PracticeHistoryModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 租户ID
    /// </summary>
    [ViewData]
    public string? TenantId { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    [ViewData]
    public string Title { get; set; } = "我的练习";

    /// <summary>
    /// GET请求处理
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>页面结果</returns>
    public IActionResult OnGet(string tenantId)
    {
        TenantId = tenantId;
        ViewData["TenantId"] = tenantId;
        
        _logger.LogInformation("用户访问我的练习页面，租户ID: {TenantId}", tenantId);
        
        return Page();
    }
} 