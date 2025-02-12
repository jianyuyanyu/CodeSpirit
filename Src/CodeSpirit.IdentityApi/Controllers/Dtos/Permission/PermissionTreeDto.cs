// Controllers/RolesController.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Permission
{
    /// <summary>
    /// DTO 类，用于表示权限树的节点。
    /// </summary>
    public class PermissionTreeDto
    {
        [Required]
        [DisplayName("节点ID")]
        public string Id { get; set; }

        [Required]
        [DisplayName("节点名称")]
        public string Label { get; set; }

        [DisplayName("子节点")]
        public List<PermissionTreeDto> Children { get; set; }
    }
}
