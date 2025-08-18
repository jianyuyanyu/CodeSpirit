using CodeSpirit.Core.Dtos;
using CodeSpirit.MultiTenant.Models;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Tenant
{
    /// <summary>
    /// 租户查询数据传输对象
    /// </summary>
    public class TenantQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [DisplayName("租户ID")]
        public string TenantId { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        [DisplayName("租户名称")]
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [DisplayName("显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 租户策略
        /// </summary>
        [DisplayName("租户策略")]
        public TenantStrategy? Strategy { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 租户域名
        /// </summary>
        [DisplayName("租户域名")]
        public string Domain { get; set; }

        /// <summary>
        /// 是否过期
        /// </summary>
        [DisplayName("是否过期")]
        public bool? IsExpired { get; set; }

        /// <summary>
        /// 创建时间开始
        /// </summary>
        [DisplayName("创建时间开始")]
        public DateTime? CreatedAtStart { get; set; }

        /// <summary>
        /// 创建时间结束
        /// </summary>
        [DisplayName("创建时间结束")]
        public DateTime? CreatedAtEnd { get; set; }
    }
} 