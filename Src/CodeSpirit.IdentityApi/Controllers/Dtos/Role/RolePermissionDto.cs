// Controllers/RolesController.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Role
{
    /// <summary>
    /// DTO 用于角色权限数据传输
    /// </summary>
    public class RolePermissionDto
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("角色ID")]
        public string RoleId { get; set; }

        [Required]
        [DisplayName("权限名称")]
        public string Name { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }

        [Required]
        [DisplayName("是否允许")]
        public bool IsAllowed { get; set; }

        [DisplayName("父级ID")]
        public int? ParentId { get; set; }

        [IgnoreColumn]
        [DisplayName("子权限")]
        public List<RolePermissionDto> Children { get; set; }
    }
}
