using CodeSpirit.MultiTenant.Models;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Tenant
{
    /// <summary>
    /// 内部租户数据传输对象
    /// 用于内部API调用，包含租户存储所需的基本信息
    /// </summary>
    public class InternalTenantDto
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
        /// 描述
        /// </summary>
        [DisplayName("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 租户策略
        /// </summary>
        [DisplayName("租户策略")]
        public TenantStrategy Strategy { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 租户域名
        /// </summary>
        [DisplayName("租户域名")]
        public string Domain { get; set; }

        /// <summary>
        /// 租户Logo URL
        /// </summary>
        [DisplayName("Logo")]
        public string LogoUrl { get; set; }

        /// <summary>
        /// 最大用户数
        /// </summary>
        [DisplayName("最大用户数")]
        public int MaxUsers { get; set; }

        /// <summary>
        /// 存储限制(MB)
        /// </summary>
        [DisplayName("存储限制(MB)")]
        public long StorageLimit { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        [DisplayName("过期时间")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 主题配置
        /// </summary>
        [DisplayName("主题配置")]
        public string ThemeConfig { get; set; }

        /// <summary>
        /// 功能配置
        /// </summary>
        [DisplayName("功能配置")]
        public string Configuration { get; set; }
    }
}
