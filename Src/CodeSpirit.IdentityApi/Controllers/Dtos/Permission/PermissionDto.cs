// Controllers/RolesController.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Permission
{
    // DTO 用于权限数据传输
    public class PermissionDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("权限名称")]
        public string Name { get; set; }

        [Required]
        [DisplayName("路径")]
        public string Path { get; set; }

        [Required]
        [DisplayName("请求方法")]
        public string RequestMethod { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }

        [DisplayName("父级权限")]
        public string Parent { get; set; }

        [Required]
        [DisplayName("权限代码")]
        public string Code { get; set; }

        [IgnoreColumn]
        [DisplayName("子权限")]
        public List<PermissionDto> Children { get; set; }
    }
}
