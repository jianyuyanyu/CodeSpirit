using CodeSpirit.LLM.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodeSpirit.LLM.Caching
{
    /// <summary>
    /// 基于内存的LLM缓存服务实现
    /// </summary>
    public class MemoryLLMCacheService : ILLMCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly LLMCacheOptions _options;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public MemoryLLMCacheService(IMemoryCache cache, IOptions<LLMCacheOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }
        
        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (!_options.Enabled)
            {
                value = default;
                return false;
            }
            
            return _cache.TryGetValue(_options.KeyPrefix + key, out value);
        }
        
        /// <inheritdoc/>
        public void SetValue<T>(string key, T value, int? absoluteExpirationMinutes = null)
        {
            if (!_options.Enabled)
            {
                return;
            }
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(
                    absoluteExpirationMinutes ?? _options.ExpirationMinutes));
            
            _cache.Set(_options.KeyPrefix + key, value, cacheEntryOptions);
        }
        
        /// <inheritdoc/>
        public bool Remove(string key)
        {
            if (!_options.Enabled)
            {
                return false;
            }
            
            _cache.Remove(_options.KeyPrefix + key);
            return true;
        }
        
        /// <inheritdoc/>
        public void Clear()
        {
            // 内存缓存不支持直接清除所有项，需要使用专门的可清除缓存实现
            // 这里只是一个占位符
        }
        
        /// <inheritdoc/>
        public string GenerateKey(object promptOrMessages, string modelId, float temperature)
        {
            string serialized;
            
            if (promptOrMessages is string prompt)
            {
                serialized = prompt;
            }
            else if (promptOrMessages is IEnumerable<ChatMessage> messages)
            {
                serialized = JsonSerializer.Serialize(messages.Select(m => new { m.Role, m.Content }));
            }
            else
            {
                serialized = JsonSerializer.Serialize(promptOrMessages);
            }
            
            // 创建唯一散列值作为缓存键
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(
                $"{serialized}_{modelId}_{temperature}"));
            
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
} 