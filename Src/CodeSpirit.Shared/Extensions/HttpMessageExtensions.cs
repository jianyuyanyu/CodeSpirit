using System.Net.Http.Json;

namespace CodeSpirit.Shared.Extensions
{
    /// <summary>
    /// http请求、响应 扩展类
    /// </summary>
    public static class HttpMessageExtensions
    {
        /// <summary>
        /// 往请求头添加租户id
        /// </summary>
        /// <param name="query"></param>
        /// <param name="tenantId"></param>
        public static HttpRequestMessage AddTenantIdToHeader(this HttpRequestMessage query, int tenantId)
        {
            query.Headers.Add("TenantId", tenantId.ToString());
            return query;
        }

        /// <summary>
        /// 读取响应体并序列化 (不区分大小写)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="responseTask"></param>
        /// <returns></returns>
        public static async Task<T> ReadFromContentJsonAsync<T>(this Task<HttpResponseMessage> responseTask)
        {
            var response = await responseTask;
            return await response.Content.ReadFromJsonAsync<T>();
        }
    }
}
