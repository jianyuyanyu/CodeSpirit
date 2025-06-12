using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 练习开始页面模型
    /// </summary>
    public class PracticeStartModel : PageModel
    {
        private readonly ILogger<PracticeStartModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public PracticeStartModel(ILogger<PracticeStartModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public IActionResult OnGet(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("练习开始页面缺少租户ID");
                return RedirectToPage("/Login");
            }

            TenantId = tenantId;
            
            // 设置基本的页面信息
            ViewData["Title"] = "练习系统";
            ViewData["TenantId"] = tenantId;
            
            return Page();
        }
    }
} 