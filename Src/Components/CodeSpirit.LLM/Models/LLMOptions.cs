using CodeSpirit.Core;

namespace CodeSpirit.LLM.Models
{
    /// <summary>
    /// LLM服务配置选项
    /// </summary>
    public class LLMOptions
    {
        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// 服务类型
        /// </summary>
        public LLMServiceType ServiceType { get; set; } = LLMServiceType.OpenAI;
        
        /// <summary>
        /// 默认模型
        /// </summary>
        public string DefaultModel { get; set; } = "gpt-4o";
        
        /// <summary>
        /// Azure OpenAI部署名称（仅当ServiceType为AzureOpenAI时使用）
        /// </summary>
        public string DeploymentName { get; set; }
        
        /// <summary>
        /// Azure OpenAI终结点URL（仅当ServiceType为AzureOpenAI时使用）
        /// </summary>
        public string Endpoint { get; set; }
        
        /// <summary>
        /// 默认温度值（0-1）
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;
        
        /// <summary>
        /// 默认最大令牌数
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 4000;
        
        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>
        /// 默认系统提示语
        /// </summary>
        public string DefaultSystemPrompt { get; set; } = "你是码灵AI助手，一个有用、诚实和无害的AI助手。";
    }
    
    /// <summary>
    /// LLM服务类型
    /// </summary>
    public enum LLMServiceType
    {
        /// <summary>
        /// OpenAI
        /// </summary>
        OpenAI,
        
        /// <summary>
        /// Azure OpenAI
        /// </summary>
        AzureOpenAI,
        
        /// <summary>
        /// Anthropic Claude
        /// </summary>
        Anthropic
    }
} 