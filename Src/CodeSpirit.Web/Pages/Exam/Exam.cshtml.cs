using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam;

/// <summary>
/// 考试页面模型
/// </summary>
public class ExamModel : PageModel
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TenantId { get; set; } = default!;

    /// <summary>
    /// 考试ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public long ExamId { get; set; }

    /// <summary>
    /// 是否为开发环境
    /// </summary>
    public bool IsDevelopment { get; private set; }

    /// <summary>
    /// 处理GET请求
    /// </summary>
    /// <returns>页面结果</returns>
    public IActionResult OnGet()
    {
        // 验证必要参数
        if (string.IsNullOrEmpty(TenantId) || ExamId <= 0)
        {
            return NotFound("考试参数无效");
        }

        // 设置环境变量
        IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        return Page();
    }
} 