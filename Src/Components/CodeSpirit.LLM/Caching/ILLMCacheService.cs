using CodeSpirit.LLM.Models;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.LLM.Caching
{
    /// <summary>
    /// LLM缓存服务接口
    /// </summary>
    public interface ILLMCacheService : ISingletonDependency
    {
        /// <summary>
        /// 尝试获取缓存值
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">输出值</param>
        /// <returns>是否成功获取</returns>
        bool TryGetValue<T>(string key, out T value);
        
        /// <summary>
        /// 设置缓存值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="absoluteExpirationMinutes">绝对过期时间（分钟）</param>
        void SetValue<T>(string key, T value, int? absoluteExpirationMinutes = null);
        
        /// <summary>
        /// 移除缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        bool Remove(string key);
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 生成缓存键
        /// </summary>
        /// <param name="promptOrMessages">提示或消息</param>
        /// <param name="modelId">模型ID</param>
        /// <param name="temperature">温度值</param>
        /// <returns>缓存键</returns>
        string GenerateKey(object promptOrMessages, string modelId, float temperature);
    }
} 