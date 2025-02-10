namespace CodeSpirit.Core.Authorization
{
    /// <summary>
    /// 权限服务接口：用于管理和查询应用的权限
    /// </summary>
    public interface IHasPermissionService
    {
        /// <summary>
        /// 检查权限代码是否存在
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        bool HasPermission(string permissionCode);
    }
}
