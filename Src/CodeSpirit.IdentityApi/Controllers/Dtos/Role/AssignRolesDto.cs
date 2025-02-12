// Controllers/UserRolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Role
{
    // DTO 用于分配角色
    public class AssignRolesDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public List<string> RoleNames { get; set; }
    }
}
