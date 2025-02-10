using System;

namespace CodeSpirit.Authorization
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
        public string Name { get; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 父级权限名称
        /// </summary>
        public string Parent { get; }

        /// <summary>
        /// 权限Code（为空则生成）
        /// </summary>
        public string Code { get; }

        public PermissionAttribute(string parent = null, string code = null)
        {
            Parent = parent;
            Code = code;
        }

        public PermissionAttribute(string name, string description, string parent = null, string code = null)
        {
            Name = name;
            Description = description;
            Parent = parent;
            Code = code;
        }
    }
}
