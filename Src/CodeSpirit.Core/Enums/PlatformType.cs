using System;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Core.Enums
{
    /// <summary>
    /// 平台类型枚举
    /// </summary>
    [Flags]
    public enum PlatformType
    {
        /// <summary>
        /// 无平台
        /// </summary>
        [Display(Name = "无平台")]
        None = 0,

        /// <summary>
        /// 系统平台
        /// </summary>
        [Display(Name = "系统平台")]
        System = 1,

        /// <summary>
        /// 租户平台
        /// </summary>
        [Display(Name = "租户平台")]
        Tenant = 2,

        /// <summary>
        /// 继承父级配置（自动从父级导航节点继承PlatformType设置）
        /// </summary>
        [Display(Name = "继承父级")]
        Inherit = 4,

        /// <summary>
        /// 系统及租户平台（两个平台都支持）
        /// </summary>
        [Display(Name = "双平台")]
        Both = System | Tenant
    }
} 