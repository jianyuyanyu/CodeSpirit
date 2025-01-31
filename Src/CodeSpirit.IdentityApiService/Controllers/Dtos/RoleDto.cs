// Controllers/RolesController.cs
using CodeSpirit.Amis.Attributes;
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

        [DisplayName("权限")]
        [ListColumn(Title = "Name", SubTitle = "Description", Placeholder = "暂无权限")]
        public List<PermissionDto> Permissions { get; set; }
    }
}
