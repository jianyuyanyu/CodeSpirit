namespace CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog
{
    public class AuditLogDto
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string QueryString { get; set; }
        public string Headers { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public double Duration { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class AuditLogQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string UserName { get; set; }
        public string EventType { get; set; }
    }
} 