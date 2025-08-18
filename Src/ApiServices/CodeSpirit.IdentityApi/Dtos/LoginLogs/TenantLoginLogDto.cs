using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.LoginLogs
{
    /// <summary>
    /// 租户平台登录日志数据传输对象（不包含租户信息）
    /// </summary>
    public class TenantLoginLogDto
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("用户ID")]
        public long? UserId { get; set; }

        [Required]
        [DisplayName("用户名")]
        public string UserName { get; set; }

        [Required]
        [DisplayName("登录时间")]
        public DateTime LoginTime { get; set; }

        [Required]
        [DisplayName("IP地址")]
        public string IPAddress { get; set; }

        [DisplayName("是否成功")]
        public bool IsSuccess { get; set; }

        [DisplayName("失败原因")]
        public string FailureReason { get; set; }
    }
} 