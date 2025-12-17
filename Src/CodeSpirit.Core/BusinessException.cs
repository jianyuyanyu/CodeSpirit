using System;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 业务逻辑异常
    /// </summary>
    public class BusinessException : AppServiceException
    {
        /// <summary>
        /// 资源键（用于本地化）
        /// </summary>
        public string? ResourceKey { get; }

        /// <summary>
        /// 参数（用于本地化消息格式化）
        /// </summary>
        public object[]? Parameters { get; }

        /// <summary>
        /// 业务逻辑异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public BusinessException(string message) : base(400, message)
        {
            ResourceKey = message;
        }

        /// <summary>
        /// 业务逻辑异常
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误消息</param>
        public BusinessException(int code, string message) : base(code, message)
        {
            ResourceKey = message;
        }

        /// <summary>
        /// 业务逻辑异常（支持资源键和参数）
        /// </summary>
        /// <param name="resourceKey">资源键</param>
        /// <param name="parameters">参数</param>
        public BusinessException(string resourceKey, params object[] parameters) 
            : base(400, resourceKey)
        {
            ResourceKey = resourceKey;
            Parameters = parameters;
        }

        /// <summary>
        /// 业务逻辑异常（支持资源键、参数和错误代码）
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="resourceKey">资源键</param>
        /// <param name="parameters">参数</param>
        public BusinessException(int code, string resourceKey, params object[] parameters) 
            : base(code, resourceKey)
        {
            ResourceKey = resourceKey;
            Parameters = parameters;
        }
    }
} 