using System.ComponentModel;

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
        [AmisColumn(Fixed = "left")]
        public string EventType { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [DisplayName("用户名")]
        [AmisColumn(Fixed = "left")]
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
        /// HTTP状态码
        /// </summary>
        [DisplayName("HTTP状态码")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 请求持续时间(毫秒)
        /// </summary>
        [DisplayName("请求持续时间(毫秒)")]
        [AmisColumn(
            BackgroundScaleMin = 0,
            BackgroundScaleMax = 10000,
            BackgroundScaleColors = new[] { "#FFEF9C", "#FF7127" })]
        public double Duration { get; set; }

        /// <summary>
        /// HTTP请求头
        /// </summary>
        [DisplayName("HTTP请求头")]
        [AmisColumn(Type = "json", Copyable = true, Toggled = false)]
        public string Headers { get; set; }

        /// <summary>
        /// 请求体
        /// </summary>
        [DisplayName("请求体")]
        [AmisColumn(Type = "json", Copyable = true, Toggled = false)]
        public string RequestBody { get; set; }

        /// <summary>
        /// 响应体
        /// </summary>
        [DisplayName("响应体")]
        [AmisColumn(Type = "json", Copyable = true, Toggled = false)]
        public string ResponseBody { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        [DisplayName("事件发生时间")]
        public DateTime EventTime { get; set; }
    }
}