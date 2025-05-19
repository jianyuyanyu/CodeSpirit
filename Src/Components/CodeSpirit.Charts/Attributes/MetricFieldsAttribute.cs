namespace CodeSpirit.Charts.Attributes;

/// <summary>
/// 度量字段特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MetricFieldsAttribute : Attribute
{
    /// <summary>
    /// 度量字段名称列表
    /// </summary>
    public string[] FieldNames { get; }

    /// <summary>
    /// 初始化度量字段特性
    /// </summary>
    /// <param name="fieldNames">字段名称列表</param>
    public MetricFieldsAttribute(params string[] fieldNames)
    {
        FieldNames = fieldNames;
    }
}