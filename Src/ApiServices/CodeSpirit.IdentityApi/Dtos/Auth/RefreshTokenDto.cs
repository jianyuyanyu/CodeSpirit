using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 刷新令牌请求数据传输对象
    /// </summary>
    public class RefreshTokenDto
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        [Required]
        [DisplayName("访问令牌")]
        public string Token { get; set; }
        
        /// <summary>
        /// 刷新令牌
        /// </summary>
        [Required]
        [DisplayName("刷新令牌")]
        public string RefreshToken { get; set; }
    }
} 