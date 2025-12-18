using System;
using System.ComponentModel;

namespace CodeSpirit.Core.Attributes;

/// <summary>
/// 支持多语言的描述特性
/// 注意：资源解析在 AMIS 表单生成时进行，而不是在属性访问时
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class LocalizedDescriptionAttribute : DescriptionAttribute
{
    /// <summary>
    /// 资源键名称
    /// </summary>
    public string ResourceKey { get; set; }

    /// <summary>
    /// 资源类型（包含ResourceManager的类）
    /// </summary>
    public Type ResourceType { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public LocalizedDescriptionAttribute()
    {
    }

    /// <summary>
    /// 构造函数（带回退描述）
    /// </summary>
    /// <param name="fallbackDescription">当资源不可用时的回退描述</param>
    public LocalizedDescriptionAttribute(string fallbackDescription) : base(fallbackDescription)
    {
    }
}
