using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 支持多租户的用户验证器
/// </summary>
public class TenantAwareUserValidator : UserValidator<ApplicationUser>
{
    private readonly ICurrentUser? _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errors">错误描述器</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public TenantAwareUserValidator(
        IdentityErrorDescriber? errors,
        ICurrentUser? currentUser,
        IHttpContextAccessor httpContextAccessor) : base(errors)
    {
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// 验证用户名在当前租户内是否唯一
    /// </summary>
    /// <param name="manager">用户管理器</param>
    /// <param name="user">要验证的用户</param>
    /// <returns>验证结果</returns>
    public override async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        // 调用基类的其他验证逻辑
        var result = await base.ValidateAsync(manager, user);
        if (!result.Succeeded)
        {
            return result;
        }

        var errors = new List<IdentityError>();

        // 验证用户名在租户内的唯一性
        await ValidateUserNameAsync(manager, user, errors);

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
    /// 验证用户名在当前租户内是否唯一
    /// </summary>
    /// <param name="manager">用户管理器</param>
    /// <param name="user">要验证的用户</param>
    /// <param name="errors">错误列表</param>
    private async Task ValidateUserNameAsync(UserManager<ApplicationUser> manager, ApplicationUser user, List<IdentityError> errors)
    {
        var userName = await manager.GetUserNameAsync(user);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        // 获取用户的租户ID（如果是新用户，使用当前租户ID）
        var userTenantId = user.TenantId ?? GetCurrentTenantId();

        // 检查在同一租户内是否存在相同用户名的其他用户
        var existingUser = await manager.Users
            .Where(u => u.NormalizedUserName == user.NormalizedUserName 
                     && u.TenantId == userTenantId 
                     && u.Id != user.Id)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            errors.Add(new IdentityError
            {
                Code = "DuplicateUserName",
                Description = $"用户名 '{userName}' 在当前租户内已存在。"
            });
        }
    }
}

