using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Tenant
{
    /// <summary>
    /// 创建租户数据传输对象
    /// </summary>
    public class TenantCreateDto
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [Required(ErrorMessage = "租户ID不能为空")]
        [StringLength(50, ErrorMessage = "租户ID长度不能超过50个字符")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "租户ID只能包含字母、数字、连字符和下划线")]
        [DisplayName("租户ID")]
        [Description("请输入租户ID，只能包含字母、数字、连字符和下划线")]
        public string TenantId { get; set; }

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
        [AmisTextareaField(Placeholder = "请输入租户描述", MinRows = 3, MaxRows = 5)]
        public string Description { get; set; }

        /// <summary>
        /// 租户策略
        /// </summary>
        [Required(ErrorMessage = "租户策略不能为空")]
        [DisplayName("租户策略")]
        public TenantStrategy Strategy { get; set; } = TenantStrategy.SharedDatabase;

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        [StringLength(1000, ErrorMessage = "连接字符串长度不能超过1000个字符")]
        [DisplayName("数据库连接字符串")]
        [AmisTextareaField(Placeholder = "请输入数据库连接字符串", MinRows = 2, MaxRows = 4, VisibleOn = "strategy == 3")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// 表前缀
        /// </summary>
        [StringLength(20, ErrorMessage = "表前缀长度不能超过20个字符")]
        [DisplayName("表前缀")]
        [AmisFormField(Type = "input-text", Placeholder = "请输入表前缀", VisibleOn = "strategy == 2")]
        public string TablePrefix { get; set; }

        /// <summary>
        /// 租户域名
        /// </summary>
        [StringLength(100, ErrorMessage = "域名长度不能超过100个字符")]
        [DisplayName("租户域名")]
        [Description("请输入租户域名，如：tenant.example.com")]
        public string Domain { get; set; }

        /// <summary>
        /// 租户Logo URL
        /// </summary>
        [StringLength(500, ErrorMessage = "Logo URL长度不能超过500个字符")]
        [DisplayName("Logo URL")]
        [AmisInputImageField()]
        public string LogoUrl { get; set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "最大用户数必须大于0")]
        [DisplayName("最大用户数")]
        [AmisNumberField(DefaultValue = 1000, Min = 1, Unit = "个", Placeholder = "请设置最大用户数限制")]
        public int MaxUsers { get; set; } = 1000;

        /// <summary>
        /// 存储空间限制（MB）
        /// </summary>
        [Range(1, long.MaxValue, ErrorMessage = "存储限制必须大于0")]
        [DisplayName("存储限制(MB)")]
        [AmisNumberField(DefaultValue = 10240, Min = 1, Unit = "MB", Placeholder = "请设置存储空间限制")]
        public long StorageLimit { get; set; } = 10240;

        /// <summary>
        /// 过期时间
        /// </summary>
        [DisplayName("过期时间")]
        [AmisDatetimeField(RelativeTime = "nextmonth", Placeholder = "请选择租户过期时间")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 租户配置（JSON格式）
        /// </summary>
        [DisplayName("租户配置")]
        [AmisFormField(Type = "json-editor", DefaultValue = "{}", Placeholder = "请输入租户配置（JSON格式）")]
        public string Configuration { get; set; } = "{}";

        /// <summary>
        /// 租户主题配置
        /// </summary>
        [DisplayName("主题配置")]
        [AmisFormField(Type = "json-editor", DefaultValue = "{}", Placeholder = "请输入主题配置（JSON格式）")]
        public string ThemeConfig { get; set; } = "{}";
    }
}