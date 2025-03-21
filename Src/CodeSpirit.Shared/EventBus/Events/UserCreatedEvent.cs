namespace CodeSpirit.Shared.EventBus.Events;

/// <summary>
/// 用户创建事件
/// </summary>
public class UserCreatedEvent
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
    public string IdNo { get; set; }
    public string UserName { get; set; }
} 