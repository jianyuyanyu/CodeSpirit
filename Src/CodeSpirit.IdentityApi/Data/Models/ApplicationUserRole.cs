using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 用户与角色的关联模型，继承自 IdentityUserRole<string>。
    /// </summary>
    public class ApplicationUserRole : IdentityUserRole<long>, IMultiTenant
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TenantId { get; set; }

        /// <summary>
        /// 关联创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationRole Role { get; set; }

        public virtual ApplicationUser User { get; set; }

        // 可以根据需求添加更多属性
    }

}
