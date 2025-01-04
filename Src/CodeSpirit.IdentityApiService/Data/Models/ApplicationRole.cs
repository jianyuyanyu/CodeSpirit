using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    [Table(nameof(ApplicationRole))]
    public class ApplicationRole : IdentityRole
    {
        // 添加自定义属性，例如描述
        [MaxLength(256)]
        public string Description { get; set; }

        /// <summary>
        /// 角色与权限的多对多关系。
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; set; }

        /// <summary>
        /// 角色与用户的多对多关系。
        /// </summary>
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
