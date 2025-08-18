// Controllers/UserRolesController.cs
namespace CodeSpirit.IdentityApi.Dtos.User
{
    // DTO 用于展示用户角色
    public class UserRolesDto
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public List<string> Roles { get; set; }
    }
}
