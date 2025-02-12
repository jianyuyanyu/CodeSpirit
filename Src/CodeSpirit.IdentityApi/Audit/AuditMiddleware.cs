using Microsoft.Extensions.Options;

namespace CodeSpirit.IdentityApi.Audit
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuditConfig _auditConfig;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(
            RequestDelegate next,
            IOptions<AuditConfig> auditConfig,
            ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _auditConfig = auditConfig.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_auditConfig.IsEnabled || ShouldSkipAudit(context))
            {
                await _next(context);
                return;
            }

            try
            {
                // 确保请求体可以多次读取
                context.Request.EnableBuffering();

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audit logging");
                throw;
            }
        }

        private bool ShouldSkipAudit(HttpContext context)
        {
            string path = context.Request.Path.Value?.ToLower();
            return _auditConfig.ExcludePaths.Any(excludePath =>
                path?.StartsWith(excludePath.ToLower()) ?? false);
        }
    }

    public static class AuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuditMiddleware>();
        }
    }
}