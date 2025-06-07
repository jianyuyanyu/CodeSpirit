using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试系统登录页面模型
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public LoginModel(ILogger<LoginModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// 页面加载处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public IActionResult OnGet(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("考试系统登录缺少租户ID");
                return RedirectToPage("/Login");
            }

            TenantId = tenantId;
            TenantName = $"租户 {tenantId}"; // 这里可以后续从数据库获取实际租户名称
            
            // 设置基本的页面信息
            ViewData["Title"] = "考试系统登录";
            ViewData["TenantId"] = tenantId;
            ViewData["TenantName"] = TenantName;
            
            return Page();
        }
    }
} 