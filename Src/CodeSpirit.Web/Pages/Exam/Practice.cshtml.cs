using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 练习页面模型
    /// </summary>
    public class PracticeModel : PageModel
    {
        private readonly ILogger<PracticeModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public PracticeModel(ILogger<PracticeModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 练习ID
        /// </summary>
        public long PracticeId { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="practiceId">练习ID</param>
        public IActionResult OnGet(string tenantId, long practiceId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("练习页面缺少租户ID");
                return RedirectToPage("/Login");
            }

            if (practiceId <= 0)
            {
                _logger.LogWarning("练习页面缺少有效的练习ID: {PracticeId}", practiceId);
                return RedirectToPage($"/{tenantId}/exam/practice");
            }

            TenantId = tenantId;
            PracticeId = practiceId;
            
            // 设置基本的页面信息
            ViewData["Title"] = "开始练习";
            ViewData["TenantId"] = tenantId;
            ViewData["PracticeId"] = practiceId;
            
            return Page();
        }
    }
} 