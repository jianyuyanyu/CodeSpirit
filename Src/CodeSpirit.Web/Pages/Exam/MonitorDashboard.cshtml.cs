using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 监考大屏页面模型
    /// </summary>
    public class MonitorDashboardModel : PageModel
    {
        private readonly ILogger<MonitorDashboardModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public MonitorDashboardModel(ILogger<MonitorDashboardModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 考试ID
        /// </summary>
        public string ExamId { get; set; } = string.Empty;

        /// <summary>
        /// 租户名称
        /// </summary>
        public string TenantName { get; set; } = string.Empty;

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        public IActionResult OnGet(string tenantId, string examId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("监考大屏缺少租户ID");
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(examId))
            {
                _logger.LogWarning("监考大屏缺少考试ID");
                return BadRequest("缺少考试ID参数");
            }

            TenantId = tenantId;
            ExamId = examId;
            TenantName = $"租户 {tenantId}"; // 这里可以后续从数据库获取实际租户名称
            
            // 设置基本的页面信息
            ViewData["Title"] = "监考大屏";
            ViewData["TenantId"] = tenantId;
            ViewData["ExamId"] = examId;
            ViewData["TenantName"] = TenantName;
            
            return Page();
        }
    }
} 