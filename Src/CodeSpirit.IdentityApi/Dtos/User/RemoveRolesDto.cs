// Controllers/UserRolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User
{
    // DTO 用于移除角色
    public class RemoveRolesDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public List<string> RoleNames { get; set; }
    }
}
