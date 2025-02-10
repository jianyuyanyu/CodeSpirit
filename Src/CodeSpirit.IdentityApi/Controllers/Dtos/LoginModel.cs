// Controllers/AuthController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 登录请求模型。
    /// </summary>
    public class LoginModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}