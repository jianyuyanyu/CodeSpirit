using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Messaging.Models;

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 系统通知
    /// </summary>
    [Display(Name = "系统通知")]
    SystemNotification = 0,

    /// <summary>
    /// 用户消息
    /// </summary>
    [Display(Name = "用户消息")]
    UserMessage = 1
} 