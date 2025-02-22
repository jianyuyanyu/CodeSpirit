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
                _logger.LogInformation("收到请求 - 路径: {Path}, 方法: {Method}, 来源: {Host}",
                    context.Request.Path, context.Request.Method, currentHost);

                if (!currentHost.Equals(LOCALHOST, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("非本地请求，跳过代理");
                    await _next(context);
                    return;
                }

                _logger.LogInformation("开始处理本地代理请求");
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
            var client = _httpClientFactory.CreateClient();
            var proxyRequest = await CreateProxyRequest(context);
            
            _logger.LogInformation("准备转发请求到: {Uri}", proxyRequest.RequestUri);

            try
            {
                using var response = await client.SendAsync(proxyRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted);

                _logger.LogInformation("代理请求完成 - 状态码: {StatusCode}, 路径: {Path}",
                    response.StatusCode, context.Request.Path);

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

        private static async Task<HttpRequestMessage> CreateProxyRequest(HttpContext context)
        {
            // 使用原始请求的路径和查询字符串
            var uri = new Uri($"https://{context.Request.Host.Value}{context.Request.Path}{context.Request.QueryString}");
            var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);

            // 复制原始请求的头部信息
            foreach (var header in context.Request.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // 复制请求体
            if (context.Request.Body != null && context.Request.Body.CanRead)
            {
                request.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType != null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
            }

            return request;
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
