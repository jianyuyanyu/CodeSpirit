// Controllers/RolesController.cs
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于角色数据传输
    public class RoleDto
    {
        public string Id { get; set; }

        [DisplayName("名称")]
        public string Name { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }

        [Column(Hidden = true)]
        public List<string> PermissionIds { get; set; }
    }
}
