// Controllers/PermissionsController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于创建权限
    public class PermissionCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string Description { get; set; }

        // 可选：父权限 ID
        public int? ParentId { get; set; }

        [Required]
        public bool IsAllowed { get; set; } = true;
    }
}
