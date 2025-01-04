// Controllers/RolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于更新角色
    public class RoleUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string Description { get; set; }

        // 可选：权限ID列表
        public List<int> PermissionIds { get; set; }
    }
}
