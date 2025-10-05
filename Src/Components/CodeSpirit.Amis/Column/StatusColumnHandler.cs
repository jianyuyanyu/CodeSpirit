using CodeSpirit.Amis.Attributes.Columns;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Column;

/// <summary>
/// 状态列处理器，专门处理状态列的配置和生成
/// </summary>
public class StatusColumnHandler
{
    /// <summary>
    /// 判断属性是否应该被处理为状态列
    /// </summary>
    /// <param name="prop">属性信息</param>
    /// <returns>如果应该处理为状态列则返回true</returns>
    public bool IsStatusColumn(PropertyInfo prop)
    {
        // 检查是否有 AmisStatusColumnAttribute 特性
        if (prop.GetCustomAttribute<AmisStatusColumnAttribute>() != null)
        {
            return true;
        }

        // 检查是否有 AmisColumnAttribute 且明确设置为 status 类型
        var columnAttr = prop.GetCustomAttribute<AmisColumnAttribute>();
        if (columnAttr != null && columnAttr.Type == "status")
        {
            return true;
        }

        // 检查是否有状态映射配置
        if (columnAttr != null && 
            (columnAttr.StatusMapping != StatusMapping.None || 
             !string.IsNullOrEmpty(columnAttr.CustomStatusMap)))
        {
            return true;
        }

        // 根据属性名称和类型进行智能推断
        return IsStatusFieldByNaming(prop);
    }

    /// <summary>
    /// 根据命名模式判断是否为状态字段
    /// </summary>
    /// <param name="prop">属性信息</param>
    /// <returns>如果符合状态字段命名模式则返回true</returns>
    private bool IsStatusFieldByNaming(PropertyInfo prop)
    {
        // 获取属性的基础类型（处理可空类型）
        Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        
        // 只有字符串、布尔值和整数类型才可能是状态字段
        if (underlyingType != typeof(string) && underlyingType != typeof(bool) && 
            underlyingType != typeof(int) && underlyingType != typeof(long) &&
            underlyingType != typeof(short) && underlyingType != typeof(byte))
        {
            return false;
        }

        string propName = prop.Name.ToLowerInvariant();

        // 检查是否包含状态相关的关键词
        return propName.Contains("status") || propName.Contains("state") ||
               propName.Contains("enabled") || propName.Contains("disabled") ||
               propName.Contains("active") || propName.Contains("inactive") ||
               propName.Contains("valid") || propName.Contains("invalid") ||
               propName.Contains("success") || propName.Contains("failed") ||
               propName.Contains("approved") || propName.Contains("rejected") ||
               propName.Contains("published") || propName.Contains("draft");
    }

    /// <summary>
    /// 应用状态列配置到列对象
    /// </summary>
    /// <param name="column">列对象</param>
    /// <param name="prop">属性信息</param>
    public void ApplyStatusColumnConfiguration(JObject column, PropertyInfo prop)
    {
        // 设置列类型为状态
        column["type"] = "status";

        // 获取状态列特性
        var statusAttr = prop.GetCustomAttribute<AmisStatusColumnAttribute>();
        var columnAttr = prop.GetCustomAttribute<AmisColumnAttribute>();

        // 优先使用 AmisStatusColumnAttribute 的配置
        var effectiveAttr = statusAttr ?? columnAttr;

        if (effectiveAttr != null)
        {
            ApplyStatusMappingToColumn(column, effectiveAttr);
        }
        else
        {
            // 如果没有明确的特性配置，尝试智能推断
            ApplyIntelligentStatusMapping(column, prop);
        }
    }

    /// <summary>
    /// 将状态映射配置应用到列对象
    /// </summary>
    /// <param name="column">列对象</param>
    /// <param name="columnAttr">列特性</param>
    private void ApplyStatusMappingToColumn(JObject column, AmisColumnAttribute columnAttr)
    {
        // 如果没有配置状态映射，则跳过
        if (columnAttr.StatusMapping == StatusMapping.None && 
            string.IsNullOrEmpty(columnAttr.CustomStatusMap))
        {
            return;
        }

        // 生成状态映射配置
        var statusConfig = GenerateStatusMappingConfig(columnAttr);
        
        // 将状态映射配置合并到列配置中
        foreach (var kvp in statusConfig)
        {
            column[kvp.Key] = JToken.FromObject(kvp.Value);
        }
    }

    /// <summary>
    /// 应用智能状态映射（当没有明确配置时）
    /// </summary>
    /// <param name="column">列对象</param>
    /// <param name="prop">属性信息</param>
    private void ApplyIntelligentStatusMapping(JObject column, PropertyInfo prop)
    {
        Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        
        // 根据属性类型应用默认映射
        if (underlyingType == typeof(bool))
        {
            ApplyBooleanStatusMapping(column);
        }
        else if (underlyingType == typeof(string))
        {
            ApplyStringStatusMapping(column, prop);
        }
        else if (IsIntegerType(underlyingType))
        {
            ApplyNumericStatusMapping(column);
        }
    }

    /// <summary>
    /// 应用布尔状态映射
    /// </summary>
    /// <param name="column">列对象</param>
    private void ApplyBooleanStatusMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["true"] = "success",
            ["false"] = "fail"
        };
        
        column["labelMap"] = new JObject
        {
            ["true"] = "是",
            ["false"] = "否"
        };
    }

    /// <summary>
    /// 应用字符串状态映射
    /// </summary>
    /// <param name="column">列对象</param>
    /// <param name="prop">属性信息</param>
    private void ApplyStringStatusMapping(JObject column, PropertyInfo prop)
    {
        string propName = prop.Name.ToLowerInvariant();

        // 根据属性名称选择合适的映射
        if (propName.Contains("method") && (propName.Contains("request") || propName.Contains("http")))
        {
            // HTTP方法映射
            ApplyHttpMethodMapping(column);
        }
        else if (propName.Contains("status") && propName.Contains("code"))
        {
            // HTTP状态码映射
            ApplyHttpStatusCodeMapping(column);
        }
        else
        {
            // 通用状态映射
            ApplyCommonStatusMapping(column);
        }
    }

    /// <summary>
    /// 应用数字状态映射
    /// </summary>
    /// <param name="column">列对象</param>
    private void ApplyNumericStatusMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["1"] = "success",
            ["0"] = "fail",
            ["-1"] = "warning",
            ["2"] = "info"
        };
        
        column["labelMap"] = new JObject
        {
            ["1"] = "成功",
            ["0"] = "失败",
            ["-1"] = "警告",
            ["2"] = "信息"
        };
    }

    /// <summary>
    /// 应用HTTP方法映射
    /// </summary>
    /// <param name="column">列对象</param>
    private void ApplyHttpMethodMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["GET"] = "info",
            ["POST"] = "success",
            ["PUT"] = "warning",
            ["DELETE"] = "danger",
            ["PATCH"] = "warning",
            ["HEAD"] = "default",
            ["OPTIONS"] = "default"
        };
        
        column["labelMap"] = new JObject
        {
            ["GET"] = "查询",
            ["POST"] = "创建",
            ["PUT"] = "更新",
            ["DELETE"] = "删除",
            ["PATCH"] = "部分更新",
            ["HEAD"] = "头部请求",
            ["OPTIONS"] = "选项请求"
        };
    }

    /// <summary>
    /// 应用HTTP状态码映射
    /// </summary>
    /// <param name="column">列对象</param>
    private void ApplyHttpStatusCodeMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["200"] = "success",
            ["201"] = "success",
            ["204"] = "success",
            ["300"] = "info",
            ["301"] = "info",
            ["302"] = "info",
            ["400"] = "warning",
            ["401"] = "warning",
            ["403"] = "warning",
            ["404"] = "warning",
            ["500"] = "danger",
            ["502"] = "danger",
            ["503"] = "danger"
        };
        
        column["labelMap"] = new JObject
        {
            ["200"] = "OK",
            ["201"] = "已创建",
            ["204"] = "无内容",
            ["301"] = "永久重定向",
            ["302"] = "临时重定向",
            ["400"] = "请求错误",
            ["401"] = "未授权",
            ["403"] = "禁止访问",
            ["404"] = "未找到",
            ["500"] = "服务器错误",
            ["502"] = "网关错误",
            ["503"] = "服务不可用"
        };
    }

    /// <summary>
    /// 应用通用状态映射
    /// </summary>
    /// <param name="column">列对象</param>
    private void ApplyCommonStatusMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["active"] = "success",
            ["enabled"] = "success",
            ["success"] = "success",
            ["inactive"] = "fail",
            ["disabled"] = "fail",
            ["fail"] = "fail",
            ["pending"] = "info",
            ["processing"] = "info",
            ["warning"] = "warning",
            ["error"] = "danger",
            ["danger"] = "danger"
        };
        
        column["labelMap"] = new JObject
        {
            ["active"] = "活跃",
            ["enabled"] = "启用",
            ["success"] = "成功",
            ["inactive"] = "非活跃",
            ["disabled"] = "禁用",
            ["fail"] = "失败",
            ["pending"] = "待处理",
            ["processing"] = "处理中",
            ["warning"] = "警告",
            ["error"] = "错误",
            ["danger"] = "危险"
        };
    }

    /// <summary>
    /// 判断是否为整数类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>如果是整数类型则返回true</returns>
    private bool IsIntegerType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || 
               type == typeof(short) || type == typeof(byte) ||
               type == typeof(uint) || type == typeof(ulong) ||
               type == typeof(ushort) || type == typeof(sbyte);
    }

    /// <summary>
    /// 生成状态映射配置
    /// </summary>
    /// <param name="columnAttr">列特性</param>
    /// <returns>状态映射配置字典</returns>
    private Dictionary<string, object> GenerateStatusMappingConfig(AmisColumnAttribute columnAttr)
    {
        var config = new Dictionary<string, object>();

        // 根据映射类型生成 map 配置
        var mapConfig = GenerateMapConfig(columnAttr.StatusMapping, columnAttr.CustomStatusMap);
        if (mapConfig != null)
        {
            config["map"] = mapConfig;
        }

        // 生成 labelMap 配置
        var labelMapConfig = GenerateLabelMapConfig(columnAttr.StatusMapping, columnAttr.StatusLabelMap);
        if (labelMapConfig != null)
        {
            config["labelMap"] = labelMapConfig;
        }

        // 添加其他状态相关配置
        if (!columnAttr.ShowStatusIcon)
        {
            config["showIcon"] = false;
        }

        if (!string.IsNullOrEmpty(columnAttr.StatusPlaceholder))
        {
            config["placeholder"] = columnAttr.StatusPlaceholder;
        }

        return config;
    }

    /// <summary>
    /// 生成状态值映射配置
    /// </summary>
    /// <param name="mapping">状态映射类型</param>
    /// <param name="customMap">自定义映射</param>
    /// <returns>映射配置对象</returns>
    private object GenerateMapConfig(StatusMapping mapping, string customMap)
    {
        // 优先使用自定义映射
        if (!string.IsNullOrEmpty(customMap))
        {
            try
            {
                return JObject.Parse(customMap);
            }
            catch
            {
                // 自定义映射解析失败，继续使用预定义映射
            }
        }

        // 根据预定义映射类型生成配置
        return mapping switch
        {
            StatusMapping.HttpStatusCode => new JObject
            {
                ["200"] = "success",
                ["201"] = "success",
                ["204"] = "success",
                ["300"] = "info",
                ["301"] = "info",
                ["302"] = "info",
                ["400"] = "warning",
                ["401"] = "warning",
                ["403"] = "warning",
                ["404"] = "warning",
                ["500"] = "danger",
                ["502"] = "danger",
                ["503"] = "danger"
            },
            StatusMapping.Boolean => new JObject
            {
                ["true"] = "success",
                ["false"] = "fail"
            },
            StatusMapping.AuditOperationType => new JObject
            {
                ["Create"] = "success",
                ["Update"] = "info",
                ["Delete"] = "danger",
                ["Query"] = "default"
            },
            StatusMapping.CommonStatus => new JObject
            {
                ["active"] = "success",
                ["enabled"] = "success",
                ["success"] = "success",
                ["inactive"] = "fail",
                ["disabled"] = "fail",
                ["fail"] = "fail",
                ["pending"] = "info",
                ["processing"] = "info",
                ["warning"] = "warning",
                ["error"] = "danger",
                ["danger"] = "danger"
            },
            StatusMapping.HttpMethod => new JObject
            {
                ["GET"] = "info",
                ["POST"] = "success",
                ["PUT"] = "warning",
                ["DELETE"] = "danger",
                ["PATCH"] = "warning",
                ["HEAD"] = "default",
                ["OPTIONS"] = "default"
            },
            StatusMapping.NumericStatus => new JObject
            {
                ["1"] = "success",
                ["0"] = "fail",
                ["-1"] = "warning",
                ["2"] = "info"
            },
            _ => null
        };
    }

    /// <summary>
    /// 生成状态标签映射配置
    /// </summary>
    /// <param name="mapping">状态映射类型</param>
    /// <param name="customLabelMap">自定义标签映射</param>
    /// <returns>标签映射配置对象</returns>
    private object GenerateLabelMapConfig(StatusMapping mapping, string customLabelMap)
    {
        // 优先使用自定义标签映射
        if (!string.IsNullOrEmpty(customLabelMap))
        {
            try
            {
                return JObject.Parse(customLabelMap);
            }
            catch
            {
                // 自定义标签映射解析失败，继续使用预定义映射
            }
        }

        // 根据预定义映射类型生成标签配置
        return mapping switch
        {
            StatusMapping.HttpStatusCode => new JObject
            {
                ["200"] = "OK",
                ["201"] = "已创建",
                ["202"] = "已接受",
                ["204"] = "无内容",
                ["301"] = "永久重定向",
                ["302"] = "临时重定向",
                ["304"] = "未修改",
                ["400"] = "请求错误",
                ["401"] = "未授权",
                ["403"] = "禁止访问",
                ["404"] = "未找到",
                ["405"] = "方法不允许",
                ["409"] = "冲突",
                ["422"] = "参数错误",
                ["429"] = "请求过多",
                ["500"] = "服务器错误",
                ["501"] = "未实现",
                ["502"] = "网关错误",
                ["503"] = "服务不可用",
                ["504"] = "网关超时"
            },
            StatusMapping.Boolean => new JObject
            {
                ["true"] = "是",
                ["false"] = "否"
            },
            StatusMapping.AuditOperationType => new JObject
            {
                ["Create"] = "创建",
                ["Update"] = "更新",
                ["Delete"] = "删除",
                ["Query"] = "查询"
            },
            StatusMapping.CommonStatus => new JObject
            {
                ["active"] = "活跃",
                ["enabled"] = "启用",
                ["success"] = "成功",
                ["inactive"] = "非活跃",
                ["disabled"] = "禁用",
                ["fail"] = "失败",
                ["pending"] = "待处理",
                ["processing"] = "处理中",
                ["warning"] = "警告",
                ["error"] = "错误",
                ["danger"] = "危险"
            },
            StatusMapping.HttpMethod => new JObject
            {
                ["GET"] = "查询",
                ["POST"] = "创建",
                ["PUT"] = "更新",
                ["DELETE"] = "删除",
                ["PATCH"] = "部分更新",
                ["HEAD"] = "头部请求",
                ["OPTIONS"] = "选项请求"
            },
            StatusMapping.NumericStatus => new JObject
            {
                ["1"] = "成功",
                ["0"] = "失败",
                ["-1"] = "警告",
                ["2"] = "信息"
            },
            _ => null
        };
    }
}
