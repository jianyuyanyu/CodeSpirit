namespace CodeSpirit.Charts.Attributes;

/// <summary>
/// 维度字段特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DimensionFieldAttribute : Attribute
{
    /// <summary>
    /// 维度字段名称
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// 初始化维度字段特性
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    public DimensionFieldAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}