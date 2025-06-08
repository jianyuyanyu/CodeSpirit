using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试应用页面模型
    /// </summary>
    public class ApplicationModel : PageModel
    {
        private readonly ILogger<ApplicationModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ApplicationModel(ILogger<ApplicationModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public IActionResult OnGet(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("考试应用页面缺少租户ID");
                return RedirectToPage("/Login");
            }

            TenantId = tenantId;
            
            // 设置基本的页面信息
            ViewData["Title"] = "考试应用";
            ViewData["TenantId"] = tenantId;
            
            return Page();
        }
    }
} 