using System;

namespace CodeSpirit.Core.Authorization
{
    /// <summary>
    /// 自定义权限特性，用于标识权限信息，可覆盖默认的名称和描述。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class PermissionAttribute : Attribute
    {
        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; }

        public string DisplayName { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 父级权限名称
        /// </summary>
        public string Parent { get; set; }
    }
}
