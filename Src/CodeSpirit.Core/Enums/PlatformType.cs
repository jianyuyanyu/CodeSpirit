using System;

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
        None = 0,

        /// <summary>
        /// 系统平台
        /// </summary>
        System = 1,

        /// <summary>
        /// 租户平台
        /// </summary>
        Tenant = 2,

        /// <summary>
        /// 系统及租户平台（两个平台都支持）
        /// </summary>
        Both = System | Tenant
    }
} 