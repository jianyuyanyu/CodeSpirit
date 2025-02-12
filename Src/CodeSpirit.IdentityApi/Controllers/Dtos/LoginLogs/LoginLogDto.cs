// Controllers/AuthController.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.LoginLogs
{
    /// <summary>
    /// 登录日志数据传输对象。
    /// </summary>
    public class LoginLogDto
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("用户ID")]
        public string UserId { get; set; }

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
