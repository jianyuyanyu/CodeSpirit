using Microsoft.AspNetCore.Http.Extensions;
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
                var currentHost = context.Request.Host.Host;

                _logger.LogInformation("收到请求 - Url: {Url}, 方法: {Method}, 来源: {Host}",
                    context.Request.GetEncodedUrl(), context.Request.Method, currentHost);

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

            // Check if the request is for a static resource
            var staticFileExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg" };
            if (staticFileExtensions.Any(ext => request.Path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
            {
                _logger.LogInformation("请求为静态资源，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }

            //// Check if the request is a JSON request
            //if (!request.ContentType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
            //{
            //    _logger.LogInformation("请求不是JSON类型，跳过代理 - 路径: {Path}", request.Path);
            //    await _next(context);
            //    return;
            //}

            //if (!request.QueryString.Value?.Contains("amis", StringComparison.OrdinalIgnoreCase) ?? true)
            //{
            //    _logger.LogInformation("请求不包含amis参数，跳过代理");
            //    await _next(context);
            //    return;
            //}

            var pathSegments = request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments == null || pathSegments.Length < 2)
            {
                await _next(context);
                return;
            }

            var serviceName = pathSegments[0];

            if (serviceName == "api")
            {
                await _next(context);
                return;
            }

            // 重构目标路径：移除开头的服务名，保留 /api 开始的部分
            var apiIndex = request.Path.Value!.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
            var targetPath = apiIndex >= 0 ? request.Path.Value[apiIndex..] : request.Path.Value;

            _logger.LogInformation("代理请求转发到服务 {ServiceName}, 路径: {TargetPath}", serviceName, targetPath);

            // 创建代理请求，使用服务名作为主机名构建完整URL
            var proxyRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri($"{context.Request.Scheme}://{serviceName}{targetPath}{context.Request.QueryString}")
            };

            // 添加 Host 头
            proxyRequest.Headers.Host = context.Request.Host.Value;
            proxyRequest.Headers.Add("proxy-host", serviceName);

            // 添加 CORS 响应头
            context.Response.Headers.Append("Access-Control-Allow-Origin", context.Request.Headers["Origin"].ToString());
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

            CopyRequestHeaders(context.Request, proxyRequest);

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(proxyRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted);

                _logger.LogInformation("代理请求完成 - 状态码: {StatusCode}, 路径: {Path}",
                    response.StatusCode, targetPath);

                await CopyResponseToContext(context, response, _logger);
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

        private static async Task CopyResponseToContext(HttpContext context, HttpResponseMessage response, ILogger<ProxyMiddleware> logger)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // 分别处理 Content Headers
            foreach (var header in response.Content.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // 读取完整的响应内容
            var responseBody = await response.Content.ReadAsByteArrayAsync();
            context.Response.ContentLength = responseBody.Length;
            await context.Response.Body.WriteAsync(responseBody);
            await context.Response.Body.FlushAsync();
        }
    }
}
