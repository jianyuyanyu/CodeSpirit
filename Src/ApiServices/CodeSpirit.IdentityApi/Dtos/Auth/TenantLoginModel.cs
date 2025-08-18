using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 租户平台登录请求模型
    /// </summary>
    public class TenantLoginModel
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [DisplayName("用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
        public string Password { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        [Required(ErrorMessage = "租户ID不能为空")]
        [DisplayName("租户ID")]
        public string TenantId { get; set; }

        /// <summary>
        /// 记住我
        /// </summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;
    }
} 