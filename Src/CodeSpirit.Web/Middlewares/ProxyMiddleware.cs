using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http.Headers;
using System.Collections.Generic;
using CodeSpirit.Aggregator.Services;
using System.Text;

namespace CodeSpirit.Web.Middlewares
{
    /// <summary>
    /// 代理中间件，负责将API请求转发到相应的微服务
    /// </summary>
    /// <remarks>
    /// 该中间件会解析请求路径，将请求转发到对应的微服务，
    /// 并支持请求聚合和响应处理功能
    /// </remarks>
    public class ProxyMiddleware
    {
        /// <summary>
        /// 下一个中间件委托
        /// </summary>
        private readonly RequestDelegate _next;
        
        /// <summary>
        /// HTTP客户端工厂，用于创建HTTP客户端
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;
        
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<ProxyMiddleware> _logger;
        
        /// <summary>
        /// 聚合器服务，用于处理响应聚合
        /// </summary>
        private readonly IAggregatorService _aggregatorService;
        
        /// <summary>
        /// 最大请求体大小限制（100MB）
        /// </summary>
        private const int MaxRequestBodySize = 100 * 1024 * 1024;

        /// <summary>
        /// 初始化代理中间件
        /// </summary>
        /// <param name="next">下一个中间件委托</param>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="aggregatorService">聚合器服务</param>
        /// <exception cref="ArgumentNullException">当任何参数为null时抛出</exception>
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

        /// <summary>
        /// 中间件调用方法，处理HTTP请求
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>异步任务</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                var currentHost = context.Request.Host.Host;

                _logger.LogInformation("收到请求 - {Method}: {Url}, 来源: {Host}",
                    context.Request.Method, context.Request.GetEncodedUrl(), currentHost);

                await HandleProxyRequest(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "代理请求处理失败 - 路径: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal Server Error");
            }
        }

        /// <summary>
        /// 处理代理请求的核心逻辑
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>异步任务</returns>
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

            // Skip proxy for local web project requests
            if (IsLocalWebProjectRequest(request))
            {
                _logger.LogInformation("本地Web项目请求，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }

            // Handle WebSocket requests (particularly for SignalR hubs) - skip proxying entirely
            if (context.WebSockets.IsWebSocketRequest || 
                request.Headers.ContainsKey("Upgrade") && request.Headers["Upgrade"] == "websocket")
            {
                _logger.LogInformation("WebSocket请求，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }

            // Special check for SignalR hub connections
            if (request.Path.StartsWithSegments("/chathub", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/hub", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/signalr", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("SignalR请求，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }

            // Skip proxy for all web project resources and requests
            // Check if it's a web project resource - paths that should not be proxied
            var webProjectPaths = new[] { "/css/", "/js/", "/images/", "/fonts/", "/lib/", "/assets/", "/Pages/" };
            if (webProjectPaths.Any(p => request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Web项目资源，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }
            
            // Check if the request is for a static resource
            var staticFileExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".map", ".woff", ".woff2", ".ttf", ".eot", ".html", ".htm" };
            if (staticFileExtensions.Any(ext => request.Path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
            {
                _logger.LogInformation("请求为静态资源，跳过代理 - 路径: {Path}", request.Path);
                await _next(context);
                return;
            }

            // 检查是否为内部接口请求，如果是则拒绝转发
            if (request.Path.Value?.Contains("/internal/", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogWarning("拒绝转发内部接口请求 - 路径: {Path}", request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("禁止访问内部接口");
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
            if (pathSegments == null || pathSegments.Length < 3)
            {
                await _next(context);
                return;
            }

            var serviceName = pathSegments[0];

            // 扩展不代理的本地路径列表
            // 增加Pages目录下的所有页面路由
            switch (serviceName)
            {
                case "api":
                case "web":
                case "_blazor":
                case "swagger":
                case "health":
                case "signalr":
                case "hubs":
                case "Login":
                case "Index":
                case "Chat":
                case "Notifications":
                case "Impersonate":
                case "Shared":
                case "export-task":
                    _logger.LogInformation("Web项目路径，跳过代理 - 路径: {Path}", request.Path);
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
                if (request.ContentLength > MaxRequestBodySize)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsync("请求体太大");
                    return;
                }
                
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
                client.Timeout = TimeSpan.FromSeconds(120);

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

        /// <summary>
        /// 复制HTTP请求头到代理请求
        /// </summary>
        /// <param name="source">源HTTP请求</param>
        /// <param name="target">目标HTTP请求消息</param>
        /// <remarks>
        /// 会跳过Host头，因为需要使用新的目标主机
        /// </remarks>
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

        /// <summary>
        /// 将HTTP响应内容复制到当前上下文
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <param name="response">HTTP响应消息</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>异步任务</returns>
        /// <remarks>
        /// 支持JSON内容聚合处理，确保正确的字符编码，
        /// 并对不同类型的内容采用适当的传输方式
        /// </remarks>
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

            // 获取和设置 Content-Type，确保包含正确的字符集
            var contentType = response.Content.Headers.ContentType;
            if (contentType != null)
            {
                // 如果没有指定 charset，并且是文本或 JSON 内容，则默认添加 UTF-8 字符集
                if (string.IsNullOrEmpty(contentType.CharSet) &&
                    (contentType.MediaType?.Contains("text", StringComparison.OrdinalIgnoreCase) == true ||
                     contentType.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true))
                {
                    contentType.CharSet = "utf-8";
                }
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

            // 分别处理 Content Headers，跳过 Content-Length 和 Transfer-Encoding
            foreach (var header in response.Content.Headers)
            {
                if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // 检查内容类型是否为JSON或文本，以决定如何处理
            bool isJsonContent = contentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
            bool isTextContent = contentType?.MediaType?.Contains("text", StringComparison.OrdinalIgnoreCase) == true;

            if (needsAggregation && isJsonContent && aggregationRules.Any())
            {
                // 使用聚合器处理 JSON 内容（保持现有逻辑）
                string jsonContent = await response.Content.ReadAsStringAsync();
                logger.LogInformation("jsonContent：{jsonContent}", jsonContent);
                try
                {
                    var aggregatedJson = await _aggregatorService.AggregateJsonContent(jsonContent, aggregationRules, context);
                    await context.Response.WriteAsync(aggregatedJson, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JSON聚合处理失败");
                    await context.Response.WriteAsync(jsonContent, Encoding.UTF8);
                }
            }
            else if (isJsonContent || isTextContent)
            {
                // 对所有 JSON 或文本内容使用字符串方式处理，确保编码正确
                string content = await response.Content.ReadAsStringAsync();
                await context.Response.WriteAsync(content, Encoding.UTF8);
            }
            else
            {
                // 对二进制内容使用流式传输
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    await responseStream.CopyToAsync(context.Response.Body);
                    await context.Response.Body.FlushAsync();
                }
            }
        }

        /// <summary>
        /// 判断请求是否为本地Web项目的请求
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <returns>如果是本地Web项目请求则返回true</returns>
        private bool IsLocalWebProjectRequest(HttpRequest request)
        {
            // 所有Razor Pages页面名称
            var razorPages = new[] { 
                "Login", "Index", "Chat", "Notifications", "Impersonate", 
                "Error", "Privacy", "Account", "Profile", "Settings", "Dashboard" 
            };

            // 获取第一个路径段
            var path = request.Path.Value ?? string.Empty;
            var firstSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

            // 检查是否是Razor Page请求（页面名称作为第一个路径段）
            if (razorPages.Any(page => firstSegment.Equals(page, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 检查请求接受类型是否为HTML（通常浏览器请求页面时会包含text/html）
            if (request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
