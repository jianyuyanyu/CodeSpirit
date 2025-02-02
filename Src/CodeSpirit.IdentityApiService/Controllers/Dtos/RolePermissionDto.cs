// Controllers/RolesController.cs
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于权限数据传输
    public class RolePermissionDto
    {
        public int Id { get; set; }

        public string RoleId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsAllowed { get; set; }

        public int? ParentId { get; set; }

        [IgnoreColumn]
        public List<RolePermissionDto> Children { get; set; }
    }
}
