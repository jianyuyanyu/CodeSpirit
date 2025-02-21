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

        public string DisplayName { get; }

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
