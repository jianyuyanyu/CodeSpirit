using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 客户端登录请求模型（支持考试系统、培训系统等）
    /// </summary>
    public class ClientLoginModel
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
        /// 客户端类型（exam: 考试系统, training: 培训系统, learning: 学习系统, assessment: 评估系统等）
        /// </summary>
        [DisplayName("客户端类型")]
        public string ClientType { get; set; } = "exam";

        /// <summary>
        /// 记住我
        /// </summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;
    }
} 