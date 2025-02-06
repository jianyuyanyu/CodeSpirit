// Controllers/RolesController.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    // DTO 用于权限数据传输
    public class PermissionDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string RequestMethod { get; set; }

        public string Description { get; set; }

        public string ParentId { get; set; }

        public string Code { get; set; }

        [IgnoreColumn]
        public List<PermissionDto> Children { get; set; }
    }
}
