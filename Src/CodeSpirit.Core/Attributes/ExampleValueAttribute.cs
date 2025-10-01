using System;

namespace CodeSpirit.Core.Attributes
{
    /// <summary>
    /// 用于标记属性的示例值特性，主要用于批量导入模板生成
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExampleValueAttribute : Attribute
    {
        /// <summary>
        /// 示例值
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 初始化示例值特性
        /// </summary>
        /// <param name="value">示例值</param>
        public ExampleValueAttribute(string value)
        {
            Value = value;
        }
    }
}

