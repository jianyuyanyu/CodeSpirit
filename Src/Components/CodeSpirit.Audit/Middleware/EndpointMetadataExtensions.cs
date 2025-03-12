using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 端点元数据扩展
/// </summary>
public static class EndpointMetadataExtensions
{
    /// <summary>
    /// 获取元数据
    /// </summary>
    public static T? GetMetadata<T>(this EndpointMetadataCollection metadata) where T : class
    {
        if (metadata == null)
            return null;

        foreach (var item in metadata)
        {
            if (item is T typedItem)
                return typedItem;
        }

        return null;
    }
} 