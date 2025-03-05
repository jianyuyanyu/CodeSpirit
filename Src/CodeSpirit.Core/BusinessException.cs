using System;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 业务逻辑异常
    /// </summary>
    public class BusinessException : AppServiceException
    {
        /// <summary>
        /// 业务逻辑异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public BusinessException(string message) : base(400, message)
        {
        }

        /// <summary>
        /// 业务逻辑异常
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误消息</param>
        public BusinessException(int code, string message) : base(code, message)
        {
        }
    }
} 