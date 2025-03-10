using CodeSpirit.Messaging.Models;
using System.ComponentModel;

namespace CodeSpirit.MessagingApi.Dtos.Requests;

/// <summary>
/// 消息查询条件
/// </summary>
public class MessageQueryDto
{
    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PerPage { get; set; } = 20;

    /// <summary>
    /// 消息类型
    /// </summary>
    [DisplayName("消息类型")]
    public MessageType? Type { get; set; }
    
    /// <summary>
    /// 消息标题（模糊查询）
    /// </summary>
    [DisplayName("标题")]
    public string? Title { get; set; }
    
    /// <summary>
    /// 发送者ID
    /// </summary>
    [DisplayName("发送者ID")]
    public string? SenderId { get; set; }
    
    /// <summary>
    /// 发送者名称（模糊查询）
    /// </summary>
    [DisplayName("发送者")]
    public string? SenderName { get; set; }
    
    /// <summary>
    /// 接收者ID
    /// </summary>
    [DisplayName("接收者ID")]
    public string? RecipientId { get; set; }
    
    /// <summary>
    /// 是否已读
    /// </summary>
    [DisplayName("是否已读")]
    public bool? IsRead { get; set; }
    
    /// <summary>
    /// 开始日期
    /// </summary>
    [DisplayName("开始日期")]
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// 结束日期
    /// </summary>
    [DisplayName("结束日期")]
    public DateTime? EndDate { get; set; }
} 