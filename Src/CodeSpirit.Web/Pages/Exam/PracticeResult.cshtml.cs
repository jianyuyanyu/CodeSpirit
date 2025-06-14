using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Web.Pages.Exam;

/// <summary>
/// 练习详情页面模型
/// </summary>
public class PracticeResultModel : PageModel
{
    private readonly ILogger<PracticeResultModel> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public PracticeResultModel(ILogger<PracticeResultModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 租户ID
    /// </summary>
    [ViewData]
    public string? TenantId { get; set; }

    /// <summary>
    /// 练习记录ID
    /// </summary>
    [ViewData]
    public string? RecordId { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    [ViewData]
    public string Title { get; set; } = "练习详情";

    /// <summary>
    /// GET请求处理
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="recordId">练习记录ID</param>
    /// <returns>页面结果</returns>
    public IActionResult OnGet(string tenantId, string recordId)
    {
        TenantId = tenantId;
        RecordId = recordId;
        ViewData["TenantId"] = tenantId;
        ViewData["RecordId"] = recordId;
        
        _logger.LogInformation("用户访问练习详情页面，租户ID: {TenantId}, 记录ID: {RecordId}", tenantId, recordId);
        
        return Page();
    }
} 