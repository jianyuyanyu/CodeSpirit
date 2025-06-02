using Microsoft.AspNetCore.Authorization;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.Core.Authorization;

/// <summary>
/// 平台权限要求类
/// </summary>
public class PlatformRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 平台类型
    /// </summary>
    public PlatformType PlatformType { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="platformType">平台类型</param>
    public PlatformRequirement(PlatformType platformType)
    {
        PlatformType = platformType;
    }
} 