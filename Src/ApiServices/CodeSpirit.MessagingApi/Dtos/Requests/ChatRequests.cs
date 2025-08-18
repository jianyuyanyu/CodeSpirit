namespace CodeSpirit.MessagingApi.Dtos.Requests;

/// <summary>
/// 创建会话请求
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public required string CreatorId { get; set; }

    /// <summary>
    /// 创建者名称
    /// </summary>
    public required string CreatorName { get; set; }

    /// <summary>
    /// 参与者ID列表
    /// </summary>
    public List<string> ParticipantIds { get; set; } = new();
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 发送者ID
    /// </summary>
    public required string SenderId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    public required string SenderName { get; set; }
}

/// <summary>
/// 添加参与者请求
/// </summary>
public class AddParticipantRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// 用户名称
    /// </summary>
    public required string UserName { get; set; }
}

/// <summary>
/// 私聊会话请求
/// </summary>
public class PrivateConversationRequest
{
    /// <summary>
    /// 用户1 ID
    /// </summary>
    public required string UserId1 { get; set; }

    /// <summary>
    /// 用户1 名称
    /// </summary>
    public required string UserName1 { get; set; }

    /// <summary>
    /// 用户2 ID
    /// </summary>
    public required string UserId2 { get; set; }

    /// <summary>
    /// 用户2 名称
    /// </summary>
    public required string UserName2 { get; set; }
}