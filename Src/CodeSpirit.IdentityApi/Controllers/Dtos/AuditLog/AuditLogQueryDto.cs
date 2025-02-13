using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog
{
    /// <summary>
    /// 审计日志查询参数
    /// </summary>
    public class AuditLogQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 当前页码，默认为1
        /// </summary>
        [DisplayName("当前页码")]
        public new int Page { get; set; } = 1;

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

        /// <summary>
        /// 操作时间范围
        /// </summary>
        [DisplayName("操作时间")]
        public DateTime[] EventTime { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [DisplayName("IP地址")]
        public string IpAddress { get; set; }

        /// <summary>
        /// 请求URL
        /// </summary>
        [DisplayName("请求URL")]
        public string Url { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        [DisplayName("HTTP方法")]
        public string Method { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        [DisplayName("状态码")]
        public int? StatusCode { get; set; }
    }
}