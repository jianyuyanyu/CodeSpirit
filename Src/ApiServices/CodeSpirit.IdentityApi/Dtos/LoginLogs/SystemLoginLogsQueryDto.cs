using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.LoginLogs
{
    /// <summary>
    /// 系统平台登录日志查询条件DTO
    /// </summary>
    public class SystemLoginLogsQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>
        /// 是否登录成功
        /// </summary>
        [DisplayName("登录结果")]
        public bool? IsSuccess { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [DisplayName("IP地址")]
        public string? IPAddress { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        [DisplayName("租户")]
        [AmisSelectField(
            Source = "${ROOT_API}/api/identity/Tenants/active",
            ValueField = "tenantId",
            LabelField = "displayName",
            Multiple = false,
            JoinValues = false,
            ExtractValue = true,
            Searchable = true,
            Clearable = true,
            Placeholder = "请选择租户"
        )]
        public string? TenantId { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        [DisplayName("失败原因")]
        public string? FailureReason { get; set; }

        /// <summary>
        /// 登录时间开始
        /// </summary>
        [DisplayName("登录时间")]
        [AmisDatetimeFieldAttribute(
            DisplayFormat = "YYYY-MM-DD HH:mm:ss",
            Clearable = true,
            InputPlaceholder = "请选择开始时间"
        )]
        public DateTime? LoginTimeStart { get; set; }

        /// <summary>
        /// 登录时间结束
        /// </summary>
        [DisplayName("-")]
        [AmisDatetimeFieldAttribute(
            DisplayFormat = "YYYY-MM-DD HH:mm:ss",
            Clearable = true,
            InputPlaceholder = "请选择结束时间"
        )]
        public DateTime? LoginTimeEnd { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public long? UserId { get; set; }
    }
} 