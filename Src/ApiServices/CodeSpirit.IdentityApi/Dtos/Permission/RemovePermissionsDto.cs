// Controllers/RolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Permission
{
    // DTO 用于移除权限
    public class RemovePermissionsDto
    {
        [Required]
        public List<int> PermissionIds { get; set; }
    }
}
