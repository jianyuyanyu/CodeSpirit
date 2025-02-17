// Controllers/RolesController.cs
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Role
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
        // 权限ID列表
        [DisplayName("权限")]
        [AmisInputTreeField(
        DataSource = "${API_HOST}/api/identity/permissions/tree",
        LabelField = "label",
        ValueField = "id",
        Multiple = true,
        JoinValues = false,
        ExtractValue = true,
        Required = true,
        Placeholder = "请选择权限"
        )]
        public List<string> PermissionIds { get; set; }
    }
}
