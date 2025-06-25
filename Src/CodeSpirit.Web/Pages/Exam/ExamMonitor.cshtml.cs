using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试监控大屏页面模型 - 基于 AmisCards 实现
    /// </summary>
    public class ExamMonitorModel : PageModel
    {
        private readonly ILogger<ExamMonitorModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ExamMonitorModel(ILogger<ExamMonitorModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 租户ID
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
        /// 考试名称
        /// </summary>
        public string ExamName { get; set; } = string.Empty;

        /// <summary>
        /// 页面GET请求处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        /// <returns>页面结果</returns>
        public IActionResult OnGet(string tenantId, string examId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("[考试监控大屏] 缺少租户ID");
                return BadRequest("缺少租户ID参数");
            }

            if (string.IsNullOrEmpty(examId))
            {
                _logger.LogWarning("[考试监控大屏] 缺少考试ID");
                return BadRequest("缺少考试ID参数");
            }

            TenantId = tenantId;
            ExamId = examId;
            
            // 在实际应用中，这里应该从数据库或API获取租户和考试信息
            // 目前使用模拟数据
            TenantName = "CodeSpirit 教育";
            ExamName = "C# 程序设计期末考试";
            
            // 设置页面信息
            ViewData["Title"] = "考试监控大屏";
            ViewData["TenantId"] = tenantId;
            ViewData["ExamId"] = examId;
            ViewData["TenantName"] = TenantName;
            ViewData["ExamName"] = ExamName;
            
            _logger.LogInformation("[考试监控大屏] 初始化监控大屏，租户ID: {TenantId}, 考试ID: {ExamId}", tenantId, examId);
            
            return Page();
        }
    }
} 