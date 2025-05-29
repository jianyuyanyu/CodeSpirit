using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 角色信息
    /// </summary>
    [Table(nameof(ApplicationRole))]
    public class ApplicationRole : IdentityRole<long>, IIsActive, IFullAuditable, IMultiTenant
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TenantId { get; set; }

        // 添加自定义属性，例如描述
        [MaxLength(256)]
        public string Description { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 角色与权限的多对多关系。
        /// </summary>
        public RolePermission RolePermission { get; set; }

        /// <summary>
        /// 角色与用户的多对多关系。
        /// </summary>
        public ICollection<ApplicationUserRole> UserRoles { get; set; }

        // IFullAuditable 属性
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
