using CodeSpirit.Core.Enums;

namespace CodeSpirit.Authorization.Extensions;

/// <summary>
/// 权限节点扩展方法
/// </summary>
public static class PermissionNodeExtensions
{
    /// <summary>
    /// 根据平台类型过滤权限树
    /// </summary>
    /// <param name="permissions">权限节点列表</param>
    /// <param name="platformType">目标平台类型</param>
    /// <returns>过滤后的权限节点列表</returns>
    public static List<PermissionNode> FilterByPlatformType(this List<PermissionNode> permissions, PlatformType platformType)
    {
        if (permissions == null || !permissions.Any())
        {
            return new List<PermissionNode>();
        }

        var filteredPermissions = new List<PermissionNode>();

        foreach (var permission in permissions)
        {
            if (IsPermissionAllowedForPlatform(permission, platformType))
            {
                // 创建权限节点的副本
                var filteredPermission = new PermissionNode(
                    permission.Name,
                    permission.Description,
                    permission.Parent,
                    permission.Path,
                    permission.RequestMethod,
                    permission.DisplayName,
                    permission.PlatformType)
                {
                    Children = permission.Children.FilterByPlatformType(platformType)
                };

                filteredPermissions.Add(filteredPermission);
            }
        }

        return filteredPermissions;
    }

    /// <summary>
    /// 获取系统平台权限
    /// </summary>
    /// <param name="permissions">权限节点列表</param>
    /// <returns>系统平台权限列表</returns>
    public static List<PermissionNode> GetSystemPermissions(this List<PermissionNode> permissions)
    {
        return permissions.FilterByPlatformType(PlatformType.System);
    }

    /// <summary>
    /// 获取租户平台权限
    /// </summary>
    /// <param name="permissions">权限节点列表</param>
    /// <returns>租户平台权限列表</returns>
    public static List<PermissionNode> GetTenantPermissions(this List<PermissionNode> permissions)
    {
        return permissions.FilterByPlatformType(PlatformType.Tenant);
    }

    /// <summary>
    /// 判断权限是否允许访问指定平台
    /// </summary>
    /// <param name="permission">权限节点</param>
    /// <param name="targetPlatform">目标平台类型</param>
    /// <returns>是否允许访问</returns>
    private static bool IsPermissionAllowedForPlatform(PermissionNode permission, PlatformType targetPlatform)
    {
        // 如果权限是Both，则所有平台都可以访问
        if (permission.PlatformType == PlatformType.Both)
        {
            return true;
        }

        // 如果目标平台是Both，则需要权限也是Both或者包含对应的平台类型
        if (targetPlatform == PlatformType.Both)
        {
            return permission.PlatformType == PlatformType.Both ||
                   permission.PlatformType == PlatformType.System ||
                   permission.PlatformType == PlatformType.Tenant;
        }

        // 精确匹配平台类型
        if (permission.PlatformType == targetPlatform)
        {
            return true;
        }

        // 使用位运算检查复合平台类型
        return (permission.PlatformType & targetPlatform) != 0;
    }
} 