using Microsoft.AspNetCore.Authorization;
using CodeSpirit.Core.Enums;
using System;

namespace CodeSpirit.Core.Authorization;

/// <summary>
/// 平台权限特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PlatformAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="platformType">平台类型</param>
    public PlatformAttribute(PlatformType platformType)
    {
        Policy = $"Platform_{platformType}";
    }
} 