using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试界面页面模型
    /// </summary>
    public class ExamModel : PageModel
    {
        private readonly ILogger<ExamModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ExamModel(ILogger<ExamModel> logger)
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
        public long ExamId { get; set; }

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        /// <returns>页面结果</returns>
        public IActionResult OnGet(string tenantId, long examId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("租户ID为空，无法访问考试页面");
                return RedirectToPage("/Index");
            }

            if (examId <= 0)
            {
                _logger.LogWarning("考试ID无效: {ExamId}，租户: {TenantId}", examId, tenantId);
                return RedirectToPage("/Exam/Application", new { tenantId });
            }

            TenantId = tenantId;
            ExamId = examId;
            IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            _logger.LogInformation("用户访问考试页面，租户: {TenantId}，考试ID: {ExamId}", tenantId, examId);

            return Page();
        }
    }
} 