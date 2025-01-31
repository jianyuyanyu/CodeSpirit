using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 应用服务调用异常
    /// </summary>
    public class AppServiceException : Exception
    {
        /// <summary>
        /// 应用服务调用异常
        /// </summary>
        /// <param name="code">状态码</param>
        /// <param name="message">消息</param>
        public AppServiceException(int code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; }
    }
}
