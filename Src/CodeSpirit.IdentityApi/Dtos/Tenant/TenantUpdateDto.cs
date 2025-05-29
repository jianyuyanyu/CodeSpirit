using CodeSpirit.MultiTenant.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Tenant
{
    /// <summary>
    /// 更新租户数据传输对象
    /// </summary>
    public class TenantUpdateDto
    {
        /// <summary>
        /// 租户名称
        /// </summary>
        [Required(ErrorMessage = "租户名称不能为空")]
        [StringLength(100, ErrorMessage = "租户名称长度不能超过100个字符")]
        [DisplayName("租户名称")]
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [StringLength(200, ErrorMessage = "显示名称长度不能超过200个字符")]
        [DisplayName("显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
        [DisplayName("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 租户策略
        /// </summary>
        [Required(ErrorMessage = "租户策略不能为空")]
        [DisplayName("租户策略")]
        public TenantStrategy Strategy { get; set; }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        [StringLength(1000, ErrorMessage = "连接字符串长度不能超过1000个字符")]
        [DisplayName("数据库连接字符串")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// 表前缀
        /// </summary>
        [StringLength(20, ErrorMessage = "表前缀长度不能超过20个字符")]
        [DisplayName("表前缀")]
        public string TablePrefix { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 租户域名
        /// </summary>
        [StringLength(100, ErrorMessage = "域名长度不能超过100个字符")]
        [DisplayName("租户域名")]
        public string Domain { get; set; }

        /// <summary>
        /// 租户Logo URL
        /// </summary>
        [StringLength(500, ErrorMessage = "Logo URL长度不能超过500个字符")]
        [DisplayName("Logo URL")]
        public string LogoUrl { get; set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "最大用户数必须大于0")]
        [DisplayName("最大用户数")]
        public int MaxUsers { get; set; }

        /// <summary>
        /// 存储空间限制（MB）
        /// </summary>
        [Range(1, long.MaxValue, ErrorMessage = "存储限制必须大于0")]
        [DisplayName("存储限制(MB)")]
        public long StorageLimit { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        [DisplayName("过期时间")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 租户配置（JSON格式）
        /// </summary>
        [DisplayName("租户配置")]
        public string Configuration { get; set; }

        /// <summary>
        /// 租户主题配置
        /// </summary>
        [DisplayName("主题配置")]
        public string ThemeConfig { get; set; }
    }
} 