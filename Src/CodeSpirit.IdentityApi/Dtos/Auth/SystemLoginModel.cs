using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 系统平台登录请求模型
    /// </summary>
    public class SystemLoginModel
    {
        /// <summary>
        /// 系统管理员用户名
        /// </summary>
        [Required(ErrorMessage = "系统管理员用户名不能为空")]
        [DisplayName("系统管理员用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
        public string Password { get; set; }

        /// <summary>
        /// 记住我
        /// </summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;
    }
} 