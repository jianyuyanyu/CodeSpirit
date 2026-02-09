using System.Text.Json;
using CodeSpirit.Amis.Attributes.Columns;

namespace CodeSpirit.Amis.Helpers;

/// <summary>
/// 状态映射辅助类
/// 根据 Amis Status 组件规范处理状态映射
/// 参考：https://aisuda.bce.baidu.com/amis/zh-CN/components/status
/// </summary>
public static class StatusMappingHelper
{
    /// <summary>
    /// 根据状态映射类型获取状态值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="mapping">映射类型</param>
    /// <param name="customMap">自定义映射（JSON格式）</param>
    /// <returns>Amis状态值</returns>
    public static string GetStatusValue(object value, StatusMapping mapping, string customMap = null)
    {
        if (value == null) return "default";

        // 优先使用自定义映射
        if (!string.IsNullOrEmpty(customMap))
        {
            try
            {
                var customMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(customMap);
                var key = value.ToString();
                if (customMapping.TryGetValue(key, out var customStatus))
                {
                    return customStatus;
                }
            }
            catch
            {
                // 自定义映射解析失败，继续使用预定义映射
            }
        }

        return mapping switch
        {
            StatusMapping.HttpStatusCode => MapHttpStatusCode(value),
            StatusMapping.Boolean => MapBoolean(value),
            StatusMapping.YesNo => MapYesNo(value),
            StatusMapping.AuditOperationType => MapAuditOperationType(value),
            StatusMapping.CommonStatus => MapCommonStatus(value),
            StatusMapping.HttpMethod => MapHttpMethod(value),
            StatusMapping.NumericStatus => MapNumericStatus(value),
            _ => "default"
        };
    }

    /// <summary>
    /// 获取状态标签文本
    /// </summary>
    /// <param name="statusValue">状态值</param>
    /// <param name="labelMap">标签映射（JSON格式）</param>
    /// <param name="originalValue">原始值</param>
    /// <returns>显示文本</returns>
    public static string GetStatusLabel(string statusValue, string labelMap, object originalValue)
    {
        // 优先使用自定义标签映射
        if (!string.IsNullOrEmpty(labelMap))
        {
            try
            {
                var labelMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(labelMap);
                if (labelMapping.TryGetValue(statusValue, out var customLabel))
                {
                    return customLabel;
                }
            }
            catch
            {
                // 标签映射解析失败，使用默认逻辑
            }
        }

        // 使用默认标签
        return GetDefaultStatusLabel(statusValue, originalValue);
    }

    /// <summary>
    /// HTTP状态码映射
    /// </summary>
    private static string MapHttpStatusCode(object value)
    {
        if (!int.TryParse(value.ToString(), out var statusCode))
            return "default";

        return statusCode switch
        {
            >= 200 and < 300 => "success",    // 2xx 成功
            >= 300 and < 400 => "info",       // 3xx 重定向
            >= 400 and < 500 => "warning",    // 4xx 客户端错误
            >= 500 and < 600 => "danger",     // 5xx 服务器错误
            _ => "default"
        };
    }

    /// <summary>
    /// 布尔值映射
    /// </summary>
    private static string MapBoolean(object value)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "success" : "fail";
        }

        var stringValue = value.ToString()?.ToLower();
        return stringValue switch
        {
            "true" or "1" or "yes" or "on" or "enabled" => "success",
            "false" or "0" or "no" or "off" or "disabled" => "fail",
            _ => "default"
        };
    }

    /// <summary>
    /// 审计操作类型映射
    /// </summary>
    private static string MapAuditOperationType(object value)
    {
        var operationType = value.ToString()?.ToLower();
        return operationType switch
        {
            "create" or "add" or "insert" => "success",
            "update" or "modify" or "edit" => "info",
            "delete" or "remove" => "danger",
            "query" or "select" or "read" or "get" => "default",
            "login" => "success",
            "logout" => "info",
            "import" => "info",
            "export" => "info",
            "batch" => "warning",
            "upload" => "info",
            "download" => "default",
            "authorize" => "warning",
            "setting" => "info",
            _ => "default"
        };
    }

    /// <summary>
    /// 通用状态映射
    /// </summary>
    private static string MapCommonStatus(object value)
    {
        var status = value.ToString()?.ToLower();
        return status switch
        {
            "active" or "enabled" or "success" or "completed" or "approved" => "success",
            "inactive" or "disabled" or "fail" or "failed" or "rejected" => "fail",
            "pending" or "processing" or "running" or "in-progress" => "info",
            "warning" or "caution" => "warning",
            "error" or "danger" or "critical" => "danger",
            "draft" or "new" => "default",
            "cancelled" or "canceled" => "secondary",
            _ => "default"
        };
    }

    /// <summary>
    /// HTTP请求方法映射
    /// </summary>
    private static string MapHttpMethod(object value)
    {
        var method = value.ToString()?.ToUpper();
        return method switch
        {
            "GET" => "info",        // 查询操作 - 蓝色
            "POST" => "success",    // 创建操作 - 绿色
            "PUT" => "warning",     // 更新操作 - 橙色
            "DELETE" => "danger",   // 删除操作 - 红色
            "PATCH" => "warning",   // 部分更新 - 橙色
            "HEAD" => "default",    // 头部请求 - 默认
            "OPTIONS" => "default", // 选项请求 - 默认
            _ => "default"
        };
    }

    /// <summary>
    /// 数字状态映射
    /// </summary>
    private static string MapNumericStatus(object value)
    {
        if (!int.TryParse(value.ToString(), out var numValue))
            return "default";

        return numValue switch
        {
            1 => "success",
            0 => "fail",
            -1 => "warning",
            2 => "info",
            _ => "default"
        };
    }

    /// <summary>
    /// 是/否值映射（中性语义）
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>AMIS 状态值</returns>
    private static string MapYesNo(object value)
    {
        // 处理 null 值
        if (value == null)
        {
            return "default";
        }
        
        if (value is bool boolValue)
        {
            return boolValue ? "info" : "default";  // info=是, default=否
        }
        
        // 字符串解析（使用不区分大小写比较以提高性能）
        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return "default";
        }
        
        if (stringValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("1", StringComparison.Ordinal) ||
            stringValue.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("是", StringComparison.Ordinal))
        {
            return "info";
        }
        
        if (stringValue.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("0", StringComparison.Ordinal) ||
            stringValue.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("否", StringComparison.Ordinal))
        {
            return "default";
        }
        
        return "default";
    }

    /// <summary>
    /// 获取默认状态标签
    /// </summary>
    private static string GetDefaultStatusLabel(string statusValue, object originalValue)
    {
        // 根据状态值返回中文标签
        var defaultLabels = new Dictionary<string, string>
        {
            ["success"] = "成功",
            ["fail"] = "失败",
            ["info"] = "信息",
            ["warning"] = "警告",
            ["danger"] = "危险",
            ["default"] = "默认",
            ["secondary"] = "次要"
        };

        if (defaultLabels.TryGetValue(statusValue, out var label))
        {
            return label;
        }

        // 如果没有找到对应标签，返回原始值
        return originalValue?.ToString() ?? statusValue;
    }

    /// <summary>
    /// 生成 Amis Status 组件配置
    /// </summary>
    /// <param name="attribute">AmisColumn特性</param>
    /// <param name="value">字段值</param>
    /// <returns>Status组件配置</returns>
    public static object GenerateStatusConfig(AmisColumnAttribute attribute, object value)
    {
        var statusValue = GetStatusValue(value, attribute.StatusMapping, attribute.CustomStatusMap);
        var statusLabel = GetStatusLabel(statusValue, attribute.StatusLabelMap, value);

        var config = new Dictionary<string, object>
        {
            ["type"] = "status",
            ["value"] = statusValue,
            ["label"] = statusLabel
        };

        // 添加可选配置
        if (!attribute.ShowStatusIcon)
        {
            config["showIcon"] = false;
        }

        if (!string.IsNullOrEmpty(attribute.StatusPlaceholder))
        {
            config["placeholder"] = attribute.StatusPlaceholder;
        }

        return config;
    }

    /// <summary>
    /// 获取所有预定义的HTTP状态码映射
    /// </summary>
    /// <returns>HTTP状态码到状态值的映射字典</returns>
    public static Dictionary<int, string> GetHttpStatusCodeMappings()
    {
        var mappings = new Dictionary<int, string>();

        // 2xx 成功状态
        for (int i = 200; i < 300; i++)
        {
            mappings[i] = "success";
        }

        // 3xx 重定向状态
        for (int i = 300; i < 400; i++)
        {
            mappings[i] = "info";
        }

        // 4xx 客户端错误
        for (int i = 400; i < 500; i++)
        {
            mappings[i] = "warning";
        }

        // 5xx 服务器错误
        for (int i = 500; i < 600; i++)
        {
            mappings[i] = "danger";
        }

        return mappings;
    }

    /// <summary>
    /// 获取常用HTTP状态码的中文描述
    /// </summary>
    /// <returns>状态码描述字典</returns>
    public static Dictionary<int, string> GetHttpStatusCodeDescriptions()
    {
        return new Dictionary<int, string>
        {
            [200] = "成功",
            [201] = "已创建",
            [204] = "无内容",
            [301] = "永久重定向",
            [302] = "临时重定向",
            [304] = "未修改",
            [400] = "请求错误",
            [401] = "未授权",
            [403] = "禁止访问",
            [404] = "未找到",
            [405] = "方法不允许",
            [409] = "冲突",
            [422] = "参数错误",
            [429] = "请求过多",
            [500] = "服务器错误",
            [502] = "网关错误",
            [503] = "服务不可用",
            [504] = "网关超时"
        };
    }
}
