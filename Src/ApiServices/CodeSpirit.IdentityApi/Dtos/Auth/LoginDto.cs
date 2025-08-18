using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 登录请求数据传输对象
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [DisplayName("用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [DisplayName("密码")]
        public string Password { get; set; }
        
        /// <summary>
        /// 租户ID
        /// </summary>
        [DisplayName("租户ID")]
        public string TenantId { get; set; }
        
        /// <summary>
        /// 客户端IP地址
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// 客户端信息
        /// </summary>
        public string UserAgent { get; set; }
    }
} 