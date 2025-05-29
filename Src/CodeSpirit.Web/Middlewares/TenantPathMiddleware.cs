using Microsoft.AspNetCore.Http;

namespace CodeSpirit.Web.Middlewares
{
    /// <summary>
    /// 租户路径解析中间件
    /// 从URL路径中解析租户信息并设置到HttpContext
    /// </summary>
    public class TenantPathMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantPathMiddleware> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="logger">日志记录器</param>
        public TenantPathMiddleware(RequestDelegate next, ILogger<TenantPathMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 中间件执行方法
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            
            // 解析路径中的租户ID
            string tenantId = null;
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    // 检查是否为租户路径格式: /tenantId/... 或 /tenant/tenantId/...
                    if (segments.Length >= 2 && segments[0].Equals("tenant", StringComparison.OrdinalIgnoreCase))
                    {
                        tenantId = segments[1];
                    }
                    else if (segments.Length >= 1 && !IsSystemPath(segments[0]))
                    {
                        // 直接使用第一段作为租户ID（排除系统路径）
                        tenantId = segments[0];
                    }
                }
            }

            // 如果解析到租户ID，设置到上下文
            if (!string.IsNullOrEmpty(tenantId))
            {
                context.Items["TenantId"] = tenantId;
                context.Request.Headers["TenantId"] = tenantId;
                
                _logger.LogDebug("从路径解析到租户: {TenantId}, 路径: {Path}", tenantId, path);
            }

            await _next(context);
        }

        /// <summary>
        /// 判断是否为系统路径（不应该被识别为租户ID）
        /// </summary>
        /// <param name="segment">路径段</param>
        /// <returns>是否为系统路径</returns>
        private bool IsSystemPath(string segment)
        {
            var systemPaths = new[]
            {
                "api", "login", "logout", "register", "error", "health", 
                "swagger", "hangfire", "admin", "public", "assets", 
                "js", "css", "images", "fonts", "favicon.ico", "client",
                "mobile", "task", "tasks", "monitor", "chat", "notifications"
            };
            
            return systemPaths.Contains(segment.ToLowerInvariant());
        }
    }
} 