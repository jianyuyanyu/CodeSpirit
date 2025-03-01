using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http.Headers;
using System.Collections.Generic;
using CodeSpirit.Web.Services;

namespace CodeSpirit.Web.Middlewares
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProxyMiddleware> _logger;
        private readonly IAggregatorService _aggregatorService;

        public ProxyMiddleware(
            RequestDelegate next,
            IHttpClientFactory httpClientFactory,
            ILogger<ProxyMiddleware> logger,
            IAggregatorService aggregatorService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aggregatorService = aggregatorService ?? throw new ArgumentNullException(nameof(aggregatorService));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                var currentHost = context.Request.Host.Host;

                _logger.LogInformation("收到请求 - {Method}: {Url}, 来源: {Host}",
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
            
            // Check if the request has the required header
            //if (!request.Headers.TryGetValue("X-Forwarded-With", out var forwardedWith) || 
            //    !forwardedWith.Equals("CodeSpirit"))
            //{
            //    await _next(context);
            //    return;
            //}
            
            // Check if the request is for a static resource
            var staticFileExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".map" };
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

            // 复制请求体
            if (request.ContentLength > 0)
            {
                // 确保请求体可以多次读取
                request.EnableBuffering();
                
                // 读取请求体
                var buffer = new byte[request.ContentLength.Value];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                
                // 重置流位置以便后续中间件可以再次读取
                request.Body.Position = 0;
                
                // 添加到代理请求
                proxyRequest.Content = new ByteArrayContent(buffer);
                
                // 如果有 Content-Type 头，也复制过来
                if (request.ContentType != null)
                {
                    proxyRequest.Content.Headers.ContentType = 
                        MediaTypeHeaderValue.Parse(request.ContentType);
                }
            }

            // 添加 Host 头
            proxyRequest.Headers.Host = context.Request.Host.Value;
            proxyRequest.Headers.Add("proxy-host", serviceName);

            // 添加 CORS 响应头
            context.Response.Headers.Append("Access-Control-Allow-Origin", context.Request.Headers["Origin"].ToString());
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

            // 添加原始IP地址
            var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIpAddress))
            {
                proxyRequest.Headers.Add("X-Forwarded-For", remoteIpAddress);
                proxyRequest.Headers.Add("X-Real-IP", remoteIpAddress);
            }

            // 添加原始协议
            proxyRequest.Headers.Add("X-Forwarded-Proto", context.Request.Scheme);

            CopyRequestHeaders(context.Request, proxyRequest);

            try
            {
                var client = _httpClientFactory.CreateClient();
                // 设置请求超时时间
                client.Timeout = TimeSpan.FromSeconds(30);
                
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

        private async Task CopyResponseToContext(HttpContext context, HttpResponseMessage response, ILogger<ProxyMiddleware> logger)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            // 检查是否需要进行聚合处理
            bool needsAggregation = _aggregatorService.NeedsAggregation(response);
            Dictionary<string, string> aggregationRules = [];
            
            if (needsAggregation)
            {
                aggregationRules = _aggregatorService.GetAggregationRules(response);
            }

            foreach (var header in response.Headers)
            {
                // 移除聚合相关的头信息，不传递给客户端
                if (header.Key.StartsWith("X-Aggregate-", StringComparison.OrdinalIgnoreCase))
                    continue;
            
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

            // 检查内容类型是否为JSON，只对JSON内容进行聚合
            bool isJsonContent = response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

            if (needsAggregation && isJsonContent && aggregationRules.Any())
            {
                // 读取JSON内容
                string jsonContent = await response.Content.ReadAsStringAsync();
                try
                {
                    // 使用聚合器服务处理JSON内容
                    var aggregatedJson = await _aggregatorService.AggregateJsonContent(jsonContent, aggregationRules, context);
                    
                    // 写入修改后的JSON到响应
                    await context.Response.WriteAsync(aggregatedJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JSON聚合处理失败");
                    // 如果JSON处理失败，回退到原始内容
                    await context.Response.WriteAsync(jsonContent);
                }
            }
            else
            {
                // 不需要聚合，使用流式传输
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    // 不设置ContentLength，使用分块传输
                    await responseStream.CopyToAsync(context.Response.Body);
                    await context.Response.Body.FlushAsync();
                }
            }
        }
    }
}
