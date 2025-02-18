// Controllers/AuthController.cs
namespace CodeSpirit.IdentityApi.Dtos.Auth
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