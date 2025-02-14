using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Profile
{
    public class ProfileDto
    {
        [DisplayName("ID")]
        public long Id { get; set; }

        [DisplayName("姓名")]
        public string Name { get; set; }

        [DisplayName("用户名")]
        public string UserName { get; set; }

        [DisplayName("电子邮箱")]
        public string Email { get; set; }

        [DisplayName("头像")]
        public string AvatarUrl { get; set; }

        [DisplayName("手机号码")]
        public string PhoneNumber { get; set; }

        [DisplayName("角色")]
        public IEnumerable<string> Roles { get; set; }

        [DisplayName("权限")]
        public IEnumerable<string> Permissions { get; set; }
    }
} 