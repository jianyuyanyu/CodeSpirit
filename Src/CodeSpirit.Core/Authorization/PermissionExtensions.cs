using System;
using System.Reflection;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;

namespace CodeSpirit.Core.Authorization
{
    /// <summary>
    /// 权限工具扩展方法
    /// </summary>
    public static class PermissionExtensions
    {
        /// <summary>
        /// 从方法信息获取权限代码
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <returns>权限代码</returns>
        public static string GetPermissionCode(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            // 获取控制器类型
            Type controllerType = methodInfo.DeclaringType;
            if (controllerType == null)
                throw new InvalidOperationException("无法获取方法所在的控制器类型");

            // 检查是否有显式的PermissionAttribute
            PermissionAttribute permissionAttr = methodInfo.GetCustomAttribute<PermissionAttribute>();
            if (permissionAttr?.Name != null)
            {
                return permissionAttr.Name;
            }

            // 获取控制器名称
            string controllerName = controllerType.Name.RemovePostFix("Controller").ToCamelCase();

            // 获取动作名称
            string actionName = methodInfo.Name.ToCamelCase();

            // 获取模块名称
            string moduleName = controllerType.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default";

            // 生成格式：{module}_{controller}_{action}
            return $"{moduleName}_{controllerName}_{actionName}";
        }
    }
}