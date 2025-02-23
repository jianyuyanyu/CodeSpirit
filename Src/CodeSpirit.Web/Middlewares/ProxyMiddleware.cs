using System.Net.Http.Headers;

namespace CodeSpirit.Web.Middlewares
{
    public class ProxyMiddleware
    {
        private const string LOCALHOST = "localhost";
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProxyMiddleware> _logger;

        public ProxyMiddleware(
            RequestDelegate next,
            IHttpClientFactory httpClientFactory,
            ILogger<ProxyMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                // 只处理 OPTIONS 请求
                if (!HttpMethods.IsOptions(context.Request.Method))
                {
                    await _next(context);
                    return;
                }

                var currentHost = context.Request.Host.Host;
                _logger.LogInformation("收到OPTIONS请求 - 路径: {Path}, 方法: {Method}, 来源: {Host}",
                    context.Request.Path, context.Request.Method, currentHost);

                if (!currentHost.Equals(LOCALHOST, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("非本地请求，跳过代理");
                    await _next(context);
                    return;
                }

                _logger.LogInformation("开始处理本地代理OPTIONS请求");
                await HandleProxyRequest(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "代理请求处理失败 - 路径: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal Server Error");
            }
        }

        private async Task HandleProxyRequest(HttpContext context)
        {
            var request = context.Request;
            
            if (!request.QueryString.Value?.Contains("amis", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                _logger.LogInformation("请求不包含amis参数，跳过代理");
                await _next(context);
                return;
            }

            var pathSegments = request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments == null || pathSegments.Length < 1)
            {
                await _next(context);
                return;
            }

            var serviceName = pathSegments[0];
            
            // 重构目标路径：移除开头的服务名，保留 /api 开始的部分
            var apiIndex = request.Path.Value!.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
            var targetPath = apiIndex >= 0 ? request.Path.Value[apiIndex..] : request.Path.Value;

            _logger.LogInformation("代理请求转发到服务 {ServiceName}, 路径: {TargetPath}", serviceName, targetPath);

            // 创建代理请求，使用相对路径
            var proxyRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(targetPath + context.Request.QueryString, UriKind.Relative)
            };
            
            CopyRequestHeaders(context.Request, proxyRequest);

            try
            {
                var client = _httpClientFactory.CreateClient(serviceName);
                using var response = await client.SendAsync(proxyRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted);

                _logger.LogInformation("代理请求完成 - 状态码: {StatusCode}, 路径: {Path}",
                    response.StatusCode, targetPath);

                await CopyResponseToContext(context, response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("代理请求超时 - 路径: {Path}, 目标URI: {Uri}",
                    context.Request.Path, proxyRequest.RequestUri);
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                await context.Response.WriteAsync("Gateway Timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "代理请求失败 - 路径: {Path}, 目标URI: {Uri}",
                    context.Request.Path, proxyRequest.RequestUri);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Service Unavailable");
            }
        }

        private static void CopyRequestHeaders(HttpRequest source, HttpRequestMessage target)
        {
            foreach (var header in source.Headers)
            {
                // 跳过 Host 头，因为我们要使用新的目标主机
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                if (!target.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                    target.Content != null)
                {
                    target.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        private static async Task CopyResponseToContext(HttpContext context, HttpResponseMessage response)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(context.Response.Body);
        }
    }
}
