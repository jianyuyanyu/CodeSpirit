using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog
{
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