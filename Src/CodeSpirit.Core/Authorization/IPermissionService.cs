namespace CodeSpirit.Core.Authorization
{
    public interface IPermissionService
    {
        /// <summary>
        /// 检查当前用户是否具有指定的权限。
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>如果具有权限，返回 true；否则返回 false。</returns>
        bool HasPermission(string permission);
    }
}
