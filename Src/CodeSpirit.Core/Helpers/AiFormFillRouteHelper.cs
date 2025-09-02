using CodeSpirit.Core.Attributes;
using System;
using System.Linq;

namespace CodeSpirit.Core.Helpers;

/// <summary>
/// AI表单填充路由辅助类
/// 提供统一的路由生成逻辑，确保服务端扫描器和前端增强器使用相同的路由规则
/// </summary>
public static class AiFormFillRouteHelper
{
    /// <summary>
    /// 推断控制器名称
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <returns>控制器名称</returns>
    public static string InferControllerName(Type dtoType)
    {
        var typeName = dtoType.Name;
        
        // 移除常见的DTO后缀
        if (typeName.EndsWith("Request"))
            typeName = typeName.Substring(0, typeName.Length - 7);
        else if (typeName.EndsWith("Dto"))
            typeName = typeName.Substring(0, typeName.Length - 3);
        
        // 移除Create、Generate、Add等前缀
        if (typeName.StartsWith("Create"))
            typeName = typeName.Substring(6);
        else if (typeName.StartsWith("Generate"))
            typeName = typeName.Substring(8);
        else if (typeName.StartsWith("Add"))
            typeName = typeName.Substring(3);
        
        // 转换为复数形式（简单规则）
        if (!typeName.EndsWith("s"))
        {
            if (typeName.EndsWith("y"))
                typeName = typeName.Substring(0, typeName.Length - 1) + "ies";
            else
                typeName += "s";
        }
        
        return typeName;
    }

    /// <summary>
    /// 推断服务名称
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <returns>服务名称</returns>
    public static string InferServiceName(Type dtoType)
    {
        var namespaceParts = dtoType.Namespace?.Split('.') ?? Array.Empty<string>();
        var apiServicePart = namespaceParts.FirstOrDefault(part => part.EndsWith("Api"));
        
        if (string.IsNullOrEmpty(apiServicePart))
            return null;
            
        // 移除CodeSpirit.前缀和Api后缀，转换为小写
        return apiServicePart
            .Replace("CodeSpirit.", "")
            .Replace("Api", "")
            .ToLowerInvariant();
    }

    /// <summary>
    /// 生成API路由（服务端中间件使用）
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <param name="aiFormFillAttr">AI填充特性</param>
    /// <returns>API路由</returns>
    public static string GenerateApiRoute(Type dtoType, AiFormFillAttribute aiFormFillAttr)
    {
        var serviceName = InferServiceName(dtoType);
        var controllerName = InferControllerName(dtoType);
        var endpoint = string.IsNullOrEmpty(aiFormFillAttr.ApiEndpoint) || aiFormFillAttr.ApiEndpoint == "ai-fill" 
            ? "ai-fill" 
            : aiFormFillAttr.ApiEndpoint;

        if (string.IsNullOrEmpty(serviceName))
        {
            // 默认路径
            return $"/api/{controllerName.ToLower()}/{endpoint}";
        }

        // 标准API路径
        return $"/api/{serviceName}/{controllerName.ToLower()}/{endpoint}";
    }

    /// <summary>
    /// 生成前端调用路由（包含服务前缀，用于路由转发）
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <param name="aiFormFillAttr">AI填充特性</param>
    /// <returns>前端调用路由</returns>
    public static string GenerateFrontendRoute(Type dtoType, AiFormFillAttribute aiFormFillAttr)
    {
        var serviceName = InferServiceName(dtoType);
        var endpoint = string.IsNullOrEmpty(aiFormFillAttr.ApiEndpoint) ? "ai-fill" : aiFormFillAttr.ApiEndpoint;

        if (string.IsNullOrEmpty(serviceName))
        {
            // 无法推断服务名称，使用默认端点
            return $"/api/ai-form-fill/{endpoint}";
        }

        // 前端路由格式：/{serviceName}/api/{serviceName}/{dtoName.ToLower()}/{endpoint}
        // 这种格式支持网关或反向代理的路由转发
        var controllerName = InferControllerName(dtoType);
        return $"/{serviceName}/api/{serviceName}/{controllerName}/{endpoint}";
    }
}
