using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages
{
    /// <summary>
    /// 租户登录页面模型
    /// </summary>
    public class TenantLoginModel : PageModel
    {
        private readonly ILogger<TenantLoginModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TenantLoginModel(ILogger<TenantLoginModel> logger)
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
                _logger.LogWarning("租户ID不能为空");
                return RedirectToPage("/Login");
            }

            TenantId = tenantId;
            
            // 设置基本的页面信息
            ViewData["Title"] = "租户登录";
            ViewData["TenantId"] = tenantId;
            
            return Page();
        }
    }
} 