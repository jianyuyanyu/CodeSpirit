using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试开始页面模型
    /// </summary>
    public class StartModel : PageModel
    {
        private readonly ILogger<StartModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public StartModel(ILogger<StartModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 考试ID
        /// </summary>
        public string ExamId { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        public IActionResult OnGet(string tenantId, string examId = null)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("考试开始页面缺少租户ID");
                return RedirectToPage("/Login");
            }

            TenantId = tenantId;
            ExamId = examId;
            
            // 设置基本的页面信息
            ViewData["Title"] = "开始考试";
            ViewData["TenantId"] = tenantId;
            ViewData["ExamId"] = examId;
            
            return Page();
        }
    }
} 