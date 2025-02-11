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

        /// <summary>
        /// 初始化权限特性，仅设置父级权限和权限码
        /// </summary>
        /// <param name="parent">父级权限名称</param>
        /// <param name="code">权限码，为空则自动生成</param>
        public PermissionAttribute(string parent = null, string code = null)
            : this(null, null, parent, code)
        {
        }

        /// <summary>
        /// 初始化权限特性，设置完整的权限信息
        /// </summary>
        /// <param name="name">权限名称</param>
        /// <param name="description">权限描述</param>
        /// <param name="parent">父级权限名称</param>
        /// <param name="code">权限码，为空则自动生成</param>
        public PermissionAttribute(string name, string description, string parent = null, string code = null)
        {
            if (!string.IsNullOrEmpty(code) && code.Length > 100)
            {
                throw new ArgumentException("权限码长度不能超过100个字符", nameof(code));
            }

            Name = name;
            Description = description;
            Parent = parent;
            Code = code;
        }
    }
}
