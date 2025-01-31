namespace CodeSpirit.Amis.Helpers.Dtos
{
    /// <summary>
    /// 封装单个API路由的信息，包括路径和HTTP方法。
    /// </summary>
    public class ApiRouteInfo
    {
        public string ApiPath { get; set; }
        public string HttpMethod { get; set; }

        public ApiRouteInfo(string apiPath, string httpMethod)
        {
            ApiPath = apiPath;
            HttpMethod = httpMethod;
        }
    }
}
