using Audit.Core;
using Audit.WebApi;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Newtonsoft.Json;

namespace CodeSpirit.IdentityApi.Audit
{
    public class CustomAuditDataProvider : AuditDataProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomAuditDataProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IHttpContextAccessor httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                ICurrentUser currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

                AuditApiAction webApiAudit = auditEvent.GetWebApiAuditAction();
                if (webApiAudit == null)
                {
                    return null;
                }

                AuditLog auditLog = new()
                {
                    EventType = auditEvent.EventType,
                    UserName = currentUser.UserName ?? "anonymous",
                    IpAddress = GetClientIpAddress(httpContextAccessor),
                    Method = webApiAudit.HttpMethod,
                    Url = webApiAudit.RequestUrl,
                    Headers = SerializeObject(webApiAudit.Headers),
                    RequestBody = SerializeObject(webApiAudit.RequestBody),
                    ResponseBody = SerializeObject(webApiAudit.ResponseBody),
                    StatusCode = webApiAudit.ResponseStatusCode,
                    Duration = auditEvent.Duration,
                    EventTime = DateTime.UtcNow,
                    UserId = currentUser.Id
                };

                dbContext.AuditLogs.Add(auditLog);
                dbContext.SaveChanges();

                return auditLog.Id;
            }
            catch (Exception ex)
            {
                // 记录错误日志但不影响主流程
                ILogger<CustomAuditDataProvider> logger = _serviceProvider.GetRequiredService<ILogger<CustomAuditDataProvider>>();
                logger.LogError(ex, "Failed to save audit log");
                return null;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            try
            {
                if (eventId == null)
                {
                    return;
                }

                using IServiceScope scope = _serviceProvider.CreateScope();
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                AuditApiAction webApiAudit = auditEvent.GetWebApiAuditAction();
                if (webApiAudit == null)
                {
                    return;
                }

                AuditLog auditLog = dbContext.AuditLogs.Find(eventId.ToString());
                if (auditLog == null)
                {
                    return;
                }

                // 更新响应相关信息
                auditLog.ResponseBody = SerializeObject(webApiAudit.ResponseBody);
                auditLog.StatusCode = webApiAudit.ResponseStatusCode;
                auditLog.Duration = auditEvent.Duration;

                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                ILogger<CustomAuditDataProvider> logger = _serviceProvider.GetRequiredService<ILogger<CustomAuditDataProvider>>();
                logger.LogError(ex, "Failed to update audit log");
            }
        }

        private string GetClientIpAddress(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "unknown";
            }

            string ip = httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ip ?? "unknown";
        }

        private string SerializeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            try
            {
                return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5
                });
            }
            catch
            {
                return "Unable to serialize object";
            }
        }
    }
}