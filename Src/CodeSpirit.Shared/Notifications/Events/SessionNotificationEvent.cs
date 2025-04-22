namespace CodeSpirit.Shared.Notifications.Events
{
    public class SessionNotificationEvent
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 消息主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public string Type {  get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 消息数据
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
