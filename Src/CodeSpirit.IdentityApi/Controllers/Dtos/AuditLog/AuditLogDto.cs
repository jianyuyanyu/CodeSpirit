namespace CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog
{
    /// <summary>
    /// 审计日志数据传输对象
    /// </summary>
    public class AuditLogDto
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [DisplayName("唯一标识")]
        public string Id { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        [DisplayName("事件类型")]
        public string EventType { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [DisplayName("用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [DisplayName("IP地址")]
        public string IpAddress { get; set; }

        /// <summary>
        /// HTTP请求方法
        /// </summary>
        [DisplayName("HTTP请求方法")]
        public string Method { get; set; }

        /// <summary>
        /// 请求URL
        /// </summary>
        [DisplayName("请求URL")]
        public string Url { get; set; }

        /// <summary>
        /// 查询字符串
        /// </summary>
        [DisplayName("查询字符串")]
        public string QueryString { get; set; }

        /// <summary>
        /// HTTP请求头
        /// </summary>
        [DisplayName("HTTP请求头")]
        public string Headers { get; set; }

        /// <summary>
        /// 请求体
        /// </summary>
        [DisplayName("请求体")]
        public string RequestBody { get; set; }

        /// <summary>
        /// 响应体
        /// </summary>
        [DisplayName("响应体")]
        public string ResponseBody { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        [DisplayName("HTTP状态码")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 请求持续时间(毫秒)
        /// </summary>
        [DisplayName("请求持续时间(毫秒)")]
        public double Duration { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        [DisplayName("事件发生时间")]
        public DateTime EventTime { get; set; }
    }

    /// <summary>
    /// 审计日志查询参数
    /// </summary>
    public class AuditLogQueryDto
    {
        /// <summary>
        /// 当前页码，默认为1
        /// </summary>
        [DisplayName("当前页码")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页显示记录数，默认为10
        /// </summary>
        [DisplayName("每页显示记录数")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 用户名过滤
        /// </summary>
        [DisplayName("用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// 事件类型过滤
        /// </summary>
        [DisplayName("事件类型")]
        public string EventType { get; set; }
    }
} 