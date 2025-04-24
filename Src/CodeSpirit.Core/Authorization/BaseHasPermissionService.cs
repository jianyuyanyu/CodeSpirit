using System;
using System.Reflection;

namespace CodeSpirit.Core.Authorization
{
    /// <summary>
    /// 权限验证服务基类
    /// </summary>
    public abstract class BaseHasPermissionService : IHasPermissionService
    {
        /// <summary>
        /// 检查权限代码是否存在
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public abstract bool HasPermission(string permissionCode);

        /// <summary>
        /// 获取指定方法的权限代码
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <returns>权限代码</returns>
        public virtual string GetPermissionCode(MethodInfo methodInfo)
        {
            return PermissionExtensions.GetPermissionCode(methodInfo);
        }

        /// <summary>
        /// 检查导航权限代码是否存在
        /// </summary>
        /// <param name="permissionCode">导航权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        /// <remarks>
        /// 导航权限仅检查一级和二级权限。
        /// 例如，对于权限 "exam_examPapers_createExamPaper"，
        /// 只会检查 "exam" 和 "exam_examPapers" 的权限。
        /// </remarks>
        public abstract bool HasNavigationPermission(string permissionCode);
    }
} 