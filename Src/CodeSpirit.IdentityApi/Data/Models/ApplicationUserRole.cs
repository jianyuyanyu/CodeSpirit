using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 用户与角色的关联模型，继承自 IdentityUserRole<string>。
    /// </summary>
    public class ApplicationUserRole : IdentityUserRole<long>
    {
        /// <summary>
        /// 关联创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationRole Role { get; set; }

        public virtual ApplicationUser User { get; set; }

        // 可以根据需求添加更多属性
    }

}
