// Controllers/RolesController.cs
using CodeSpirit.Amis.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于权限数据传输
    public class PermissionDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsAllowed { get; set; }

        public int? ParentId { get; set; }

        [IgnoreColumn]
        public List<PermissionDto> Children { get; set; }
    }
}
