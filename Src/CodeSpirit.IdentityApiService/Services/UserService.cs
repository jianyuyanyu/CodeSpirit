using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Repositories;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        IDistributedCache cache,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _cache = cache;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto)
    {
        // 获取 IQueryable 类型的用户数据
        var query = _userRepository.GetUsersQueryable();

        // 应用过滤、排序、分页等
        var filteredQuery = ApplyFilters(query, queryDto);

        // 获取过滤后的总数
        var totalCount = await filteredQuery.CountAsync();

        // 分页
        var pagedQuery = filteredQuery.ApplyPaging(queryDto);

        // 获取数据并映射到 DTO
        var userDtos = _mapper.Map<List<UserDto>>(await pagedQuery.ToListAsync());

        return new ListData<UserDto>(userDtos, totalCount);
    }

    private IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query, UserQueryDto queryDto)
    {
        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            var searchLower = queryDto.Keywords.ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower) ||
                u.IdNo.Contains(queryDto.Keywords) ||
                u.UserName.ToLower().Contains(searchLower));
        }

        // 应用用户是否激活过滤
        if (queryDto.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == queryDto.IsActive.Value);
        }

        // 应用性别过滤
        if (queryDto.Gender.HasValue)
        {
            query = query.Where(u => u.Gender == queryDto.Gender.Value);
        }

        // 应用角色过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Role))
        {
            var roleName = queryDto.Role.Trim();
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
        }

        // 应用最后登录时间范围过滤
        if (queryDto.LastLoginTime != null && queryDto.LastLoginTime.Length == 2)
        {
            var startDate = queryDto.LastLoginTime[0];
            query = query.Where(u => u.LastLoginTime >= startDate);

            var endDate = queryDto.LastLoginTime[1];
            query = query.Where(u => u.LastLoginTime <= endDate);
        }

        // 返回过滤后的查询
        return query;
    }

    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<(IdentityResult, string)> CreateUserAsync(CreateUserDto createUserDto)
    {
        var user = _mapper.Map<ApplicationUser>(createUserDto);

        var newPassword = PasswordGenerator.GenerateRandomPassword(12);
        var result = await _userManager.CreateAsync(user, newPassword);
        if (!result.Succeeded)
            return (result, null);

        if (createUserDto.Roles != null && createUserDto.Roles.Any())
        {
            var roleResult = await AssignRolesAsync(user, createUserDto.Roles);
            if (!roleResult.Succeeded)
                return (roleResult, null);
        }
        return (IdentityResult.Success, user.Id);
    }

    public async Task<IdentityResult> AssignRolesAsync(string id, List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

        return await AssignRolesAsync(user, roles);
    }

    public async Task<IdentityResult> RemoveRolesAsync(string id, List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

        var userRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = roles.Intersect(userRoles).ToList();

        if (!rolesToRemove.Any())
            return IdentityResult.Failed(new IdentityError { Description = "用户不具备指定的角色。" });

        return await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
    }

    private async Task<IdentityResult> AssignRolesAsync(ApplicationUser user, List<string> roles)
    {
        var validRoles = new List<string>();

        foreach (var role in roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                validRoles.Add(role);
            }
        }

        if (!validRoles.Any())
        {
            return IdentityResult.Failed(new IdentityError { Description = "没有有效的角色可分配。" });
        }

        return await _userManager.AddToRolesAsync(user, validRoles);
    }

    public async Task<IdentityResult> UpdateUserAsync(string id, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

        // 使用 AutoMapper 更新用户属性
        _mapper.Map(updateUserDto, user);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return updateResult;

        if (updateUserDto.Roles != null)
        {
            var roleResult = await UpdateUserRolesAsync(user, updateUserDto.Roles);
            if (!roleResult.Succeeded)
                return roleResult;
        }

        return IdentityResult.Success;
    }

    private async Task<IdentityResult> UpdateUserRolesAsync(ApplicationUser user, List<string> newRoles)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = newRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(newRoles).ToList();

        IdentityResult result = IdentityResult.Success;

        if (rolesToAdd.Any())
        {
            var addResult = await AssignRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
                return addResult;
        }

        if (rolesToRemove.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                return removeResult;
        }

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(string id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });

        return await _userRepository.DeleteUserAsync(user);
    }

    public async Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });

        if (user.IsActive == isActive)
        {
            var message = isActive
                ? "用户已处于激活状态，无需重复操作。"
                : "用户已处于禁用状态，无需重复操作。";
            return IdentityResult.Failed(new IdentityError { Description = message });
        }

        user.IsActive = isActive;
        return await _userManager.UpdateAsync(user);
    }

    public async Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(string id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            throw new AppServiceException(400, "账户不存在或已被禁用，请启用后再试！");

        var newPassword = PasswordGenerator.GenerateRandomPassword(12);
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        return resetResult.Succeeded ? (true, newPassword) : (false, null);
    }

    public async Task<IdentityResult> UnlockUserAsync(string id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });

        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.UtcNow)
            return IdentityResult.Failed(new IdentityError { Description = "该用户未被锁定，无需解锁" });

        await _userManager.SetLockoutEndDateAsync(user, null);
        user.AccessFailedCount = 0;
        return await _userManager.UpdateAsync(user);
    }

    public async Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> ids)
    {
        if (ids == null || !ids.Any())
        {
            return [];
        }

        // 从数据库中查询指定 IDs 的用户信息
        var users = await _userManager.Users
            .Where(u => ids.Contains(u.Id))  // 根据 ID 过滤
            .Include(u => u.UserRoles)  // 包括用户角色
                .ThenInclude(ur => ur.Role)  // 包括角色信息
            .ToListAsync();

        // 如果没有找到任何用户，返回空列表
        if (users == null || !users.Any())
        {
            return [];
        }

        return users;
    }

    public async Task QuickSaveUsersAsync(QuickSaveRequestDto request)
    {
        if (request?.Rows == null || !request.Rows.Any())
        {
            throw new AppServiceException(400, "请求数据无效或为空！");
        }

        // 获取需要更新的用户ID列表
        var userIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
        var usersToUpdate = await GetUsersByIdsAsync(userIdsToUpdate);
        if (usersToUpdate.Count != userIdsToUpdate.Count)
        {
            throw new AppServiceException(400, "部分用户未找到!");
        }

        // 3. 执行批量更新：更新 `rowsDiff` 中的变化字段
        foreach (var rowDiff in request.RowsDiff)
        {
            var user = usersToUpdate.FirstOrDefault(u => u.Id == rowDiff.Id);
            if (user != null)
            {
                // 更新变化字段（仅更新在 rowsDiff 中的字段）
                if (rowDiff.IsActive.HasValue)
                {
                    user.IsActive = rowDiff.IsActive.Value;
                }

                // 可以根据需求增加更多字段的更新
            }
        }

        // 4. 保存更新结果
        var updateResult = await _userRepository.SaveChangesAsync();
        if (updateResult == 0)
        {
            throw new AppServiceException(400, "没有更新!");
        }
    }

    #region 数据统计
    /// <summary>
    /// 用户增长趋势图（折线图/柱状图）
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var query = _userManager.Users
            .Where(u => u.CreationTime >= startDate.Date && u.CreationTime <= endDate.Date);

        // 按天统计用户注册数量
        var dailyGrowth = await query
            .GroupBy(u => u.CreationTime.Date)
            .Select(g => new { Date = g.Key, UserCount = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync();

        // 返回前端所需格式
        var result = dailyGrowth.Select(g => new UserGrowthDto
        {
            Date = g.Date,
            UserCount = g.UserCount
        }).ToList();

        return result;
    }

    /// <summary>
    /// 活跃用户统计图（柱状图/漏斗图）
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var query = _userManager.Users
            .Where(u => u.LastLoginTime >= startDate && u.LastLoginTime <= endDate);

        // 按天统计活跃用户数量
        var dailyActiveUsers = await query
            .GroupBy(u => u.LastLoginTime.Value.Date)
            .Select(g => new { Date = g.Key, ActiveUserCount = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync();

        // 返回前端所需格式
        var result = dailyActiveUsers.Select(g => new ActiveUserDto
        {
            Date = g.Date,
            ActiveUserCount = g.ActiveUserCount
        }).ToList();

        return result;
    }
    #endregion
}