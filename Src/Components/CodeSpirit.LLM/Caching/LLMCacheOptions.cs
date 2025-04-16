namespace CodeSpirit.LLM.Caching
{
    /// <summary>
    /// LLM缓存选项
    /// </summary>
    public class LLMCacheOptions
    {
        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int ExpirationMinutes { get; set; } = 30;
        
        /// <summary>
        /// 缓存键前缀
        /// </summary>
        public string KeyPrefix { get; set; } = "llm_cache_";
        
        /// <summary>
        /// 最大缓存项数
        /// </summary>
        public int MaxItems { get; set; } = 1000;
    }
} 