// Controllers/RolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Permission
{
    // DTO 用于分配权限
    public class AssignPermissionsDto
    {
        [Required]
        public List<int> PermissionIds { get; set; }
    }
}
