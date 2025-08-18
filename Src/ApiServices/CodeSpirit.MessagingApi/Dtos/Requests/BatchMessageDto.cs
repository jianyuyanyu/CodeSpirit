namespace CodeSpirit.MessagingApi.Dtos.Requests;

/// <summary>
/// 批量删除消息请求
/// </summary>
public class BatchDeleteMessagesDto
{
    /// <summary>
    /// 要删除的消息ID列表
    /// </summary>
    public required List<Guid> Ids { get; set; }
}

/// <summary>
/// 批量标记消息已读请求
/// </summary>
public class BatchMarkAsReadDto
{
    /// <summary>
    /// 要标记为已读的消息ID列表
    /// </summary>
    public required List<Guid> Ids { get; set; }
} 