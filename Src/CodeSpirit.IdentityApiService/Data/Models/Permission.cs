namespace CodeSpirit.IdentityApi.Data.Models
{
    // Models/Permission.cs
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace RoleManagementApiIdentity.Models
    {
        /// <summary>
        /// 权限模型，表示系统中的一个权限。
        /// 支持权限的层级结构和与角色的多对多关系。
        /// </summary>
        public class Permission
        {
            /// <summary>
            /// 权限的唯一标识。
            /// </summary>
            [Key]
            public int Id { get; set; }

            /// <summary>
            /// 权限名称，唯一且必填。
            /// </summary>
            [Required]
            [MaxLength(100)]
            public string Name { get; set; }

            /// <summary>
            /// 权限描述。
            /// </summary>
            [MaxLength(256)]
            public string Description { get; set; }

            /// <summary>
            /// 指示权限是允许（true）还是拒绝（false）。
            /// </summary>
            public bool IsAllowed { get; set; } = true; // 默认允许

            /// <summary>
            /// 父权限的外键，用于权限的层级结构。
            /// </summary>
            public int? ParentId { get; set; }

            /// <summary>
            /// 导航属性，指向父权限。
            /// </summary>
            [ForeignKey("ParentId")]
            public Permission Parent { get; set; }

            /// <summary>
            /// 导航属性，指向子权限。
            /// </summary>
            public ICollection<Permission> Children { get; set; }

            /// <summary>
            /// 权限与角色的多对多关系。
            /// </summary>
            public ICollection<RolePermission> RolePermissions { get; set; }
        }
    }

}


