using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages;

/// <summary>
/// 通知页面模型
/// </summary>
public class NotificationsModel : PageModel
{
    /// <summary>
    /// 当前用户ID
    /// </summary>
    public string UserId { get; set; } = "user1"; // 模拟用户ID，实际应从身份验证系统获取
    
    /// <summary>
    /// 页面处理方法
    /// </summary>
    public void OnGet()
    {
        // 从身份验证系统获取当前用户ID
        // UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
} 