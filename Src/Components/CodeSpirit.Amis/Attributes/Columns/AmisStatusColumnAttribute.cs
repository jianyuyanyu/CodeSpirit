using System.ComponentModel;

namespace CodeSpirit.Amis.Attributes.Columns;

/// <summary>
/// AMIS 状态列特性，专门用于配置状态列的显示
/// 继承自 AmisColumnAttribute，提供状态列的专用配置
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AmisStatusColumnAttribute : AmisColumnAttribute
{
    /// <summary>
    /// 初始化 AmisStatusColumnAttribute 的新实例
    /// </summary>
    public AmisStatusColumnAttribute()
    {
        // 默认设置为状态列类型
        Type = "status";
    }

    /// <summary>
    /// 初始化 AmisStatusColumnAttribute 的新实例
    /// </summary>
    /// <param name="statusMapping">状态映射类型</param>
    public AmisStatusColumnAttribute(StatusMapping statusMapping)
    {
        Type = "status";
        StatusMapping = statusMapping;
    }

    /// <summary>
    /// 初始化 AmisStatusColumnAttribute 的新实例
    /// </summary>
    /// <param name="customStatusMap">自定义状态映射（JSON格式）</param>
    public AmisStatusColumnAttribute(string customStatusMap)
    {
        Type = "status";
        CustomStatusMap = customStatusMap;
    }

    /// <summary>
    /// 初始化 AmisStatusColumnAttribute 的新实例
    /// </summary>
    /// <param name="statusMapping">状态映射类型</param>
    /// <param name="statusLabelMap">状态标签映射（JSON格式）</param>
    public AmisStatusColumnAttribute(StatusMapping statusMapping, string statusLabelMap)
    {
        Type = "status";
        StatusMapping = statusMapping;
        StatusLabelMap = statusLabelMap;
    }

    /// <summary>
    /// 快速创建布尔状态列
    /// </summary>
    /// <returns>配置为布尔映射的状态列特性</returns>
    public static AmisStatusColumnAttribute Boolean()
    {
        return new AmisStatusColumnAttribute(StatusMapping.Boolean);
    }

    /// <summary>
    /// 快速创建HTTP状态码列
    /// </summary>
    /// <returns>配置为HTTP状态码映射的状态列特性</returns>
    public static AmisStatusColumnAttribute HttpStatusCode()
    {
        return new AmisStatusColumnAttribute(StatusMapping.HttpStatusCode);
    }

    /// <summary>
    /// 快速创建HTTP请求方法列
    /// </summary>
    /// <returns>配置为HTTP方法映射的状态列特性</returns>
    public static AmisStatusColumnAttribute HttpMethod()
    {
        return new AmisStatusColumnAttribute(StatusMapping.HttpMethod);
    }

    /// <summary>
    /// 快速创建审计操作类型列
    /// </summary>
    /// <returns>配置为审计操作类型映射的状态列特性</returns>
    public static AmisStatusColumnAttribute AuditOperationType()
    {
        return new AmisStatusColumnAttribute(StatusMapping.AuditOperationType);
    }

    /// <summary>
    /// 快速创建通用状态列
    /// </summary>
    /// <returns>配置为通用状态映射的状态列特性</returns>
    public static AmisStatusColumnAttribute CommonStatus()
    {
        return new AmisStatusColumnAttribute(StatusMapping.CommonStatus);
    }

    /// <summary>
    /// 快速创建数字状态列
    /// </summary>
    /// <returns>配置为数字状态映射的状态列特性</returns>
    public static AmisStatusColumnAttribute NumericStatus()
    {
        return new AmisStatusColumnAttribute(StatusMapping.NumericStatus);
    }

    /// <summary>
    /// 创建自定义状态映射列
    /// </summary>
    /// <param name="customMap">自定义状态映射（JSON格式）</param>
    /// <param name="customLabelMap">自定义标签映射（JSON格式）</param>
    /// <returns>配置为自定义映射的状态列特性</returns>
    public static AmisStatusColumnAttribute Custom(string customMap, string customLabelMap = null)
    {
        return new AmisStatusColumnAttribute(customMap)
        {
            StatusLabelMap = customLabelMap
        };
    }
}
