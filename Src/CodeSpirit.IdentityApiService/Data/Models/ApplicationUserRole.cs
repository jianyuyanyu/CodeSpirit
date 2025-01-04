using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 用户与角色的关联模型，继承自 IdentityUserRole<string>。
    /// 可以在此处添加额外的属性，如关联时间等。
    /// </summary>
    [Table(nameof(ApplicationUserRole))]
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        /// <summary>
        /// 关联创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //public ApplicationRole Role { get; set; }
        //public ApplicationUser User { get; set; }

        // 可以根据需求添加更多属性
    }

}
