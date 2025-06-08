using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试结果页面模型
    /// </summary>
    public class ResultModel : PageModel
    {
        private readonly ILogger<ResultModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ResultModel(ILogger<ResultModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 考试记录ID
        /// </summary>
        public long RecordId { get; set; }

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="id">考试记录ID</param>
        /// <returns>页面结果</returns>
        public IActionResult OnGet(string tenantId, long id)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("租户ID为空，无法访问考试结果页面");
                return RedirectToPage("/Index");
            }

            if (id <= 0)
            {
                _logger.LogWarning("考试记录ID无效: {RecordId}，租户: {TenantId}", id, tenantId);
                return RedirectToPage("/Exam/Application", new { tenantId });
            }

            TenantId = tenantId;
            RecordId = id;
            IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            _logger.LogInformation("用户访问考试结果页面，租户: {TenantId}，记录ID: {RecordId}", tenantId, id);

            return Page();
        }
    }
} 