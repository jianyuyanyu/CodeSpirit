using System;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 数据验证异常
    /// </summary>
    public class ValidationException : AppServiceException
    {
        /// <summary>
        /// 数据验证异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public ValidationException(string message) : base(400, message)
        {
        }

        /// <summary>
        /// 数据验证异常
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误消息</param>
        public ValidationException(int code, string message) : base(code, message)
        {
        }
    }
} 