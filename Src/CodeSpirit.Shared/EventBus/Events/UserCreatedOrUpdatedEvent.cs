using System.Reflection;

namespace CodeSpirit.Shared.EventBus.Events;

/// <summary>
/// 用户创建或更新事件（支持租户隔离）
/// </summary>
public class UserCreatedOrUpdatedEvent : TenantAwareEventBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }
    
    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 手机号码
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    public string IdNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 性别
    /// </summary>
    public string Gender { get; set; } = string.Empty;
} 