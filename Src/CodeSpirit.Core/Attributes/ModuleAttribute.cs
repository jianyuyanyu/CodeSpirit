using System;

namespace CodeSpirit.Core.Attributes
{
    /// <summary>
    /// 模块特性：用于标记控制器或动作方法所属的模块。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 模块显示名称（回退文本）
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 显示名称的资源键名称（用于多语言支持）
        /// </summary>
        public string DisplayNameResourceKey { get; set; }

        /// <summary>
        /// 显示名称的资源类型（包含 ResourceManager 的类）
        /// </summary>
        public Type DisplayNameResourceType { get; set; }

        public string Icon { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">模块名称</param>
        public ModuleAttribute(string name, string displayName = null)
        {
            Name = name;
            DisplayName = displayName ?? name;
        }
    }
}
