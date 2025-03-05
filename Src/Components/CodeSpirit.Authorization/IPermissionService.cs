using CodeSpirit.Core.Authorization;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 权限服务接口：用于管理和查询应用的权限
    /// </summary>
    public interface IPermissionService : IHasPermissionService
    {
        /// <summary>
        /// 获取权限树，即所有控制器及其下属动作组成的节点集合
        /// </summary>
        /// <returns>权限树根节点列表</returns>
        List<PermissionNode> GetPermissionTree();
        bool HasPermission(string permissionName, ISet<string> userPermissions);
        Task InitializePermissionTree();
    }
}