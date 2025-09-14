using System;

namespace CodeSpirit.Core.Constants
{
    /// <summary>
    /// 缓存键值常量
    /// </summary>
    public static class CacheKeys
    {
        #region 权限缓存

        /// <summary>
        /// 用户权限缓存键格式（必须包含租户ID），参数：用户ID、租户ID
        /// </summary>
        public const string UserPermissions = "UserPermissions:{0}:Tenant:{1}";

        /// <summary>
        /// 用户权限缓存过期时间 - 绝对过期时间（小时）
        /// </summary>
        public static readonly TimeSpan UserPermissionsAbsoluteExpiration = TimeSpan.FromHours(12);

        /// <summary>
        /// 用户权限缓存过期时间 - 滑动过期时间（分钟）
        /// </summary>
        public static readonly TimeSpan UserPermissionsSlidingExpiration = TimeSpan.FromMinutes(60);

        #endregion

        #region 用户信息缓存

        /// <summary>
        /// 用户信息缓存键格式，参数：用户ID
        /// </summary>
        public const string UserInfo = "UserInfo:{0}";

        /// <summary>
        /// 用户角色缓存键格式，参数：用户ID
        /// </summary>
        public const string UserRoles = "UserRoles:{0}";

        #endregion

        #region 角色权限缓存

        /// <summary>
        /// 角色权限缓存键格式，参数：角色ID
        /// </summary>
        public const string RolePermissions = "RolePermissions:{0}";

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成用户权限缓存键（必须包含租户ID）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="tenantId">租户ID（必需）</param>
        /// <returns>缓存键</returns>
        public static string GetUserPermissionsCacheKey(long userId, string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("租户ID不能为空", nameof(tenantId));
            }
            return string.Format(UserPermissions, userId, tenantId);
        }

        /// <summary>
        /// 生成用户信息缓存键
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>缓存键</returns>
        public static string GetUserInfoCacheKey(long userId)
        {
            return string.Format(UserInfo, userId);
        }

        /// <summary>
        /// 生成用户角色缓存键
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>缓存键</returns>
        public static string GetUserRolesCacheKey(long userId)
        {
            return string.Format(UserRoles, userId);
        }

        /// <summary>
        /// 生成角色权限缓存键
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>缓存键</returns>
        public static string GetRolePermissionsCacheKey(long roleId)
        {
            return string.Format(RolePermissions, roleId);
        }

        #endregion
    }
}
