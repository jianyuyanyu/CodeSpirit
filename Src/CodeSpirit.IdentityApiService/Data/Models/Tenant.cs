using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 租户
    /// </summary>
    public class Tenant : Entity<int>, IFullEntityEvent, IIsActive
    {
        /// <summary>
        /// 租户编码
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Logo Url
        /// </summary>
        [MaxLength(225)]
        public string Logo { get; set; }

        public bool IsActive { get; set; }
    }
}
