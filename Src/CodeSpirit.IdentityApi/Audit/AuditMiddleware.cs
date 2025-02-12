using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Text;

namespace CodeSpirit.IdentityApi.Audit
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuditConfig _config;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(
            RequestDelegate next,
            IOptions<AuditConfig> config,
            ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _config = config.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_config.IsEnabled || ShouldSkipPath(context.Request))
            {
                await _next(context);
                return;
            }

            // 克隆请求头和请求体以便审计
            var originalBody = context.Request.Body;
            var originalHeaders = new Dictionary<string, StringValues>(context.Request.Headers);

            try
            {
                // 处理请求头
                FilterHeaders(context.Request.Headers);

                // 处理请求体
                context.Request.EnableBuffering();
                string bodyContent = await ReadAndFilterRequestBody(context.Request);

                // 继续处理请求
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audit logging");
                throw;
            }
            finally
            {
                // 恢复原始请求
                context.Request.Body = originalBody;
                context.Request.Headers.Clear();
                foreach (var header in originalHeaders)
                {
                    context.Request.Headers[header.Key] = header.Value;
                }
            }
        }

        private bool ShouldSkipPath(HttpRequest request)
        {
            return _config.ExcludePaths?.Any(path => 
                request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        private void FilterHeaders(IHeaderDictionary headers)
        {
            foreach (var headerToExclude in _config.ExcludeHeaders)
            {
                if (headers.ContainsKey(headerToExclude))
                {
                    headers[headerToExclude] = "[FILTERED]";
                }
            }
        }

        private async Task<string> ReadAndFilterRequestBody(HttpRequest request)
        {
            if (!request.Body.CanRead)
                return string.Empty;

            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync();
            request.Body.Position = 0;  // 重置位置以供后续中间件读取

            if (string.IsNullOrEmpty(bodyContent))
                return string.Empty;

            try
            {
                var jobject = JObject.Parse(bodyContent);
                FilterSensitiveFields(jobject);
                return jobject.ToString();
            }
            catch
            {
                return bodyContent;
            }
        }

        private void FilterSensitiveFields(JObject jobject)
        {
            foreach (var field in _config.ExcludeBodyFields)
            {
                var properties = jobject.SelectTokens($"$..{field}").ToList();
                foreach (var prop in properties)
                {
                    if (prop.Parent is JProperty parent)
                    {
                        parent.Value = "[FILTERED]";
                    }
                }
            }
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