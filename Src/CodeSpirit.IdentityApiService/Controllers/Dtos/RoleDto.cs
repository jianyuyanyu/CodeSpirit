// Controllers/RolesController.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于角色数据传输
    public class RoleDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<PermissionDto> Permissions { get; set; }
    }
}
