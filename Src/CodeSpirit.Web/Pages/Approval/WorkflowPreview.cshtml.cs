using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Approval;

/// <summary>
/// 工作流预览页面模型
/// </summary>
public class WorkflowPreviewModel : PageModel
{
    private readonly ILogger<WorkflowPreviewModel> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public WorkflowPreviewModel(ILogger<WorkflowPreviewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 租户ID（可选，从路由获取）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 工作流ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public long Id { get; set; }

    /// <summary>
    /// 工作流ID（用于前端）
    /// </summary>
    public long WorkflowId => Id;

    /// <summary>
    /// 有效的租户ID（用于前端）
    /// </summary>
    public string EffectiveTenantId => string.IsNullOrEmpty(TenantId) ? string.Empty : TenantId;

    /// <summary>
    /// 页面加载
    /// </summary>
    /// <returns>页面结果</returns>
    public IActionResult OnGet()
    {
        // 简单的页面加载，所有数据获取都在前端JavaScript中处理
        _logger.LogInformation("工作流预览页面加载: TenantId={TenantId}, WorkflowId={WorkflowId}", TenantId, Id);
        return Page();
    }
}
