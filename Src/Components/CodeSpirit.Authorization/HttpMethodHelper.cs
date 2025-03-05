using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// HTTP方法辅助类
    /// </summary>
    public static class HttpMethodHelper
    {
        /// <summary>
        /// HTTP方法映射字典
        /// </summary>
        private static readonly Dictionary<Type, string> HttpMethodMap = new()
        {
            { typeof(HttpGetAttribute), "GET" },
            { typeof(HttpPostAttribute), "POST" },
            { typeof(HttpPutAttribute), "PUT" },
            { typeof(HttpDeleteAttribute), "DELETE" },
            { typeof(HttpPatchAttribute), "PATCH" }
        };

        /// <summary>
        /// 获取动作方法的HTTP请求方法
        /// </summary>
        /// <param name="action">动作方法信息</param>
        /// <returns>HTTP方法名称</returns>
        public static string GetRequestMethod(MethodInfo action)
        {
            return HttpMethodMap.FirstOrDefault(x => action.IsDefined(x.Key, false)).Value ?? string.Empty;
        }
    }
}
