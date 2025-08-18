// Controllers/AuthController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 登录请求模型。
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// 租户ID（可选，也可以从请求头获取）
        /// </summary>
        public string TenantId { get; set; }
    }
}