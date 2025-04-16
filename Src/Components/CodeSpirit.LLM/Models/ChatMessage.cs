namespace CodeSpirit.LLM.Models
{
    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 消息角色
        /// </summary>
        public ChatRole Role { get; set; }
        
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
    }
    
    /// <summary>
    /// 聊天角色
    /// </summary>
    public enum ChatRole
    {
        /// <summary>
        /// 用户
        /// </summary>
        User,
        
        /// <summary>
        /// 助手
        /// </summary>
        Assistant,
        
        /// <summary>
        /// 系统
        /// </summary>
        System,
        
        /// <summary>
        /// 工具
        /// </summary>
        Tool
    }
    
    /// <summary>
    /// 聊天响应
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// 响应内容
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 使用的模型
        /// </summary>
        public string Model { get; set; }
        
        /// <summary>
        /// 使用的令牌数
        /// </summary>
        public int? TokensUsed { get; set; }
    }
    
    /// <summary>
    /// 聊天选项
    /// </summary>
    public class ChatOptions
    {
        /// <summary>
        /// 模型ID
        /// </summary>
        public string ModelId { get; set; }
        
        /// <summary>
        /// 温度（0-1）
        /// </summary>
        public float Temperature { get; set; } = 0.7f;
        
        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 4000;
        
        /// <summary>
        /// 系统提示语
        /// </summary>
        public string SystemPrompt { get; set; }
    }
} 