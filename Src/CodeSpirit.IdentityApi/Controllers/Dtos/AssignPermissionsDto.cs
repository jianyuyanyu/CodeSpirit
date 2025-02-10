// Controllers/RolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于分配权限
    public class AssignPermissionsDto
    {
        [Required]
        public List<int> PermissionIds { get; set; }
    }
}
