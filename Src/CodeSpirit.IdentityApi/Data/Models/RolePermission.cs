using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core;
/// <summary>
/// 角色与权限的关联实体，用于表示角色拥有的权限及其允许状态。
/// </summary>
public class RolePermission : IMultiTenant
{
    public int Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; }

    /// <summary>
    /// 角色的唯一标识。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 导航属性，指向角色。
    /// </summary>
    public ApplicationRole Role { get; set; }

    /// <summary>
    /// 权限的唯一标识数组。
    /// </summary>
    [MaxLength(5000)]
    public string[] PermissionIds { get; set; }
}
