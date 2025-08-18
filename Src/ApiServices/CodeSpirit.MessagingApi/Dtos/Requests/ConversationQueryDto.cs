using System.ComponentModel;

namespace CodeSpirit.MessagingApi.Dtos.Requests;

/// <summary>
/// 会话查询条件
/// </summary>
public class ConversationQueryDto
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
    /// 标题（模糊查询）
    /// </summary>
    [DisplayName("标题")]
    public string? Title { get; set; }
    
    /// <summary>
    /// 参与者ID
    /// </summary>
    [DisplayName("参与者ID")]
    public string? ParticipantId { get; set; }
    
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