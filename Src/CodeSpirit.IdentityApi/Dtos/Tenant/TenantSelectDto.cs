using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Tenant
{
    /// <summary>
    /// 租户选择数据传输对象
    /// </summary>
    public class TenantSelectDto
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
        /// Logo URL
        /// </summary>
        [DisplayName("Logo URL")]
        public string LogoUrl { get; set; }
    }
} 