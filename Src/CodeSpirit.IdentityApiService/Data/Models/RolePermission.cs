using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using CodeSpirit.IdentityApi.Data.Models;
/// <summary>
/// 角色与权限的关联实体，用于表示角色拥有的权限及其允许状态。
/// </summary>
public class RolePermission
{
    /// <summary>
    /// 角色的唯一标识。
    /// </summary>
    public string RoleId { get; set; }

    /// <summary>
    /// 导航属性，指向角色。
    /// </summary>
    public ApplicationRole Role { get; set; }

    /// <summary>
    /// 权限的唯一标识。
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// 导航属性，指向权限。
    /// </summary>
    public Permission Permission { get; set; }

    /// <summary>
    /// 指示权限是允许（true）还是拒绝（false）给定角色。
    /// </summary>
    public bool IsAllowed { get; set; } = true; // 默认允许
}


