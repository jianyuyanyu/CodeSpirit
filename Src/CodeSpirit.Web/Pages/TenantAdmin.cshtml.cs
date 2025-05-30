using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages
{
    /// <summary>
    /// 租户后台管理页面模型
    /// </summary>
    public class TenantAdminModel : PageModel
    {
        private readonly ILogger<TenantAdminModel> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TenantAdminModel(ILogger<TenantAdminModel> logger)
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

            
            // 这里可以添加租户权限验证逻辑
            
            TenantId = tenantId;
            
            // 设置页面信息
            ViewData["Title"] = "租户管理后台";
            ViewData["TenantId"] = tenantId;
            
            _logger.LogInformation("用户 {UserId} 访问租户 {TenantId} 的管理后台", 
                User.Identity?.Name, tenantId);
            
            return Page();
        }
    }
} 