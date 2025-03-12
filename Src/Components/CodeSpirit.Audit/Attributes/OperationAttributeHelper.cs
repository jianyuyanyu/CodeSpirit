using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Audit.Attributes;

/// <summary>
/// 操作特性辅助类
/// </summary>
public static class OperationAttributeHelper
{
    /// <summary>
    /// 提取操作特性信息
    /// </summary>
    public static Dictionary<string, string> ExtractOperationInfo(OperationAttribute operationAttr)
    {
        var result = new Dictionary<string, string>();
        
        // 获取特性的所有公共属性
        var properties = operationAttr.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            // 获取属性值
            var value = property.GetValue(operationAttr);
            
            // 只添加非空的属性
            if (value != null)
            {
                // 将属性值转换为字符串
                string strValue = value.ToString();
                
                // 对于布尔类型，如果是true才添加
                if (property.PropertyType == typeof(bool))
                {
                    if ((bool)value)
                    {
                        result.Add(property.Name, strValue);
                    }
                }
                else if (!string.IsNullOrEmpty(strValue))
                {
                    result.Add(property.Name, strValue);
                }
            }
        }
        
        return result;
    }
} 