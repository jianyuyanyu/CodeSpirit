using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 支持多租户的角色验证器
/// </summary>
public class TenantAwareRoleValidator : RoleValidator<ApplicationRole>
{
    private readonly ICurrentUser? _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errors">错误描述器</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public TenantAwareRoleValidator(
        IdentityErrorDescriber? errors,
        ICurrentUser? currentUser,
        IHttpContextAccessor httpContextAccessor) : base(errors)
    {
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// 验证角色名在当前租户内是否唯一
    /// </summary>
    /// <param name="manager">角色管理器</param>
    /// <param name="role">要验证的角色</param>
    /// <returns>验证结果</returns>
    public override async Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role)
    {
        // 调用基类的其他验证逻辑
        var result = await base.ValidateAsync(manager, role);
        if (!result.Succeeded)
        {
            return result;
        }

        var errors = new List<IdentityError>();

        // 验证角色名在租户内的唯一性
        await ValidateRoleNameAsync(manager, role, errors);

        return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }

    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    /// <returns>租户ID</returns>
    private string GetCurrentTenantId()
    {
        // 优先从CurrentUser获取租户ID
        var tenantId = _currentUser?.TenantId;
        
        // 如果CurrentUser中没有，尝试从HttpContext获取
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = _httpContextAccessor?.HttpContext?.Items["TenantId"] as string;
        }
        
        // 如果仍然没有，使用默认租户ID
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = "default";
        }
        
        return tenantId;
    }

    /// <summary>
    /// 验证角色名在当前租户内是否唯一
    /// </summary>
    /// <param name="manager">角色管理器</param>
    /// <param name="role">要验证的角色</param>
    /// <param name="errors">错误列表</param>
    private async Task ValidateRoleNameAsync(RoleManager<ApplicationRole> manager, ApplicationRole role, List<IdentityError> errors)
    {
        var roleName = await manager.GetRoleNameAsync(role);
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        // 获取角色的租户ID（如果是新角色，使用当前租户ID）
        var roleTenantId = role.TenantId ?? GetCurrentTenantId();

        // 检查在同一租户内是否存在相同角色名的其他角色
        var existingRole = await manager.Roles
            .Where(r => r.NormalizedName == role.NormalizedName 
                     && r.TenantId == roleTenantId 
                     && r.Id != role.Id)
            .FirstOrDefaultAsync();

        if (existingRole != null)
        {
            errors.Add(new IdentityError
            {
                Code = "DuplicateRoleName",
                Description = $"角色名 '{roleName}' 在当前租户内已存在。"
            });
        }
    }
}

