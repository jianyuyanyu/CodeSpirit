using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos.User;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Repositories;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _userManager = userManager;
        _roleManager = roleManager;
        this._logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto)
    {
        // 获取 IQueryable 类型的用户数据
        IQueryable<ApplicationUser> query = _userRepository.GetUsersQueryable();

        // 应用过滤、排序、分页等
        IQueryable<ApplicationUser> filteredQuery = ApplyFilters(query, queryDto);

        // 获取过滤后的总数
        int totalCount = await filteredQuery.CountAsync();

        // 分页
        IQueryable<ApplicationUser> pagedQuery = filteredQuery
            .ApplySorting(queryDto)  // 自定义排序扩展方法
                .ApplyPaging(queryDto);  // 自定义分页扩展方法

        // 获取数据并映射到 DTO
        List<UserDto> userDtos = _mapper.Map<List<UserDto>>(await pagedQuery.ToListAsync());

        return new ListData<UserDto>(userDtos, totalCount);
    }

    private IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query, UserQueryDto queryDto)
    {
        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
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
            string roleName = queryDto.Role.Trim();
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
        }

        // 应用最后登录时间范围过滤
        if (queryDto.LastLoginTime != null && queryDto.LastLoginTime.Length == 2)
        {
            DateTime startDate = queryDto.LastLoginTime[0];
            query = query.Where(u => u.LastLoginTime >= startDate);

            DateTime endDate = queryDto.LastLoginTime[1];
            query = query.Where(u => u.LastLoginTime <= endDate);
        }

        // 返回过滤后的查询
        return query;
    }

    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        ApplicationUser user = await _userRepository.GetUserByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<(IdentityResult, string)> CreateUserAsync(CreateUserDto createUserDto)
    {
        ApplicationUser user = _mapper.Map<ApplicationUser>(createUserDto);

        string newPassword = PasswordGenerator.GenerateRandomPassword(12);
        IdentityResult result = await _userManager.CreateAsync(user, newPassword);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        if (createUserDto.Roles != null && createUserDto.Roles.Any())
        {
            IdentityResult roleResult = await AssignRolesAsync(user, createUserDto.Roles);
            if (!roleResult.Succeeded)
            {
                return (roleResult, null);
            }
        }
        return (IdentityResult.Success, user.Id);
    }

    public async Task<IdentityResult> AssignRolesAsync(string id, List<string> roles)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(id);
        return user == null ? IdentityResult.Failed(new IdentityError { Description = "用户不存在！" }) : await AssignRolesAsync(user, roles);
    }

    public async Task<IdentityResult> RemoveRolesAsync(string id, List<string> roles)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });
        }

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        List<string> rolesToRemove = roles.Intersect(userRoles).ToList();

        return !rolesToRemove.Any()
            ? IdentityResult.Failed(new IdentityError { Description = "用户不具备指定的角色。" })
            : await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
    }

    private async Task<IdentityResult> AssignRolesAsync(ApplicationUser user, List<string> roles)
    {
        List<string> validRoles = [];

        foreach (string role in roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                validRoles.Add(role);
            }
        }

        return !validRoles.Any()
            ? IdentityResult.Failed(new IdentityError { Description = "没有有效的角色可分配。" })
            : await _userManager.AddToRolesAsync(user, validRoles);
    }

    public async Task<IdentityResult> UpdateUserAsync(string id, UpdateUserDto updateUserDto)
    {
        ApplicationUser user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });
        }

        // 使用 AutoMapper 更新用户属性
        _mapper.Map(updateUserDto, user);

        IdentityResult updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        if (updateUserDto.Roles != null)
        {
            IdentityResult roleResult = await UpdateUserRolesAsync(user, updateUserDto.Roles);
            if (!roleResult.Succeeded)
            {
                return roleResult;
            }
        }

        return IdentityResult.Success;
    }

    private async Task<IdentityResult> UpdateUserRolesAsync(ApplicationUser user, List<string> newRoles)
    {
        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        List<string> rolesToAdd = newRoles.Except(currentRoles).ToList();
        List<string> rolesToRemove = currentRoles.Except(newRoles).ToList();

        IdentityResult result = IdentityResult.Success;

        if (rolesToAdd.Any())
        {
            IdentityResult addResult = await AssignRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        if (rolesToRemove.Any())
        {
            IdentityResult removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(string id)
    {
        ApplicationUser user = await _userRepository.GetUserByIdAsync(id);
        return user == null
            ? IdentityResult.Failed(new IdentityError { Description = "用户不存在" })
            : await _userRepository.DeleteUserAsync(user);
    }

    public async Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive)
    {
        ApplicationUser user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });
        }

        if (user.IsActive == isActive)
        {
            string message = isActive
                ? "用户已处于激活状态，无需重复操作。"
                : "用户已处于禁用状态，无需重复操作。";
            return IdentityResult.Failed(new IdentityError { Description = message });
        }

        user.IsActive = isActive;
        return await _userManager.UpdateAsync(user);
    }

    public async Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(string id)
    {
        ApplicationUser user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            throw new AppServiceException(400, "账户不存在或已被禁用，请启用后再试！");
        }

        string newPassword = PasswordGenerator.GenerateRandomPassword(12);
        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        return resetResult.Succeeded ? (true, newPassword) : (false, null);
    }

    public async Task<IdentityResult> UnlockUserAsync(string id)
    {
        ApplicationUser user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });
        }

        DateTimeOffset? lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.Now)
        {
            return IdentityResult.Failed(new IdentityError { Description = "该用户未被锁定，无需解锁" });
        }

        // 先重置锁定结束时间
        IdentityResult setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!setLockoutResult.Succeeded)
        {
            return setLockoutResult;
        }

        // 重置访问失败次数
        user.AccessFailedCount = 0;
        IdentityResult updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        // 确保锁定功能启用
        return await _userManager.SetLockoutEnabledAsync(user, true);
    }

    public async Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> ids)
    {
        if (ids == null || !ids.Any())
        {
            return [];
        }

        // 从数据库中查询指定 IDs 的用户信息
        List<ApplicationUser> users = await _userManager.Users
            .Where(u => ids.Contains(u.Id))  // 根据 ID 过滤
            .Include(u => u.UserRoles)  // 包括用户角色
                .ThenInclude(ur => ur.Role)  // 包括角色信息
            .ToListAsync();

        // 如果没有找到任何用户，返回空列表
        return users == null || !users.Any() ? [] : users;
    }

    public async Task QuickSaveUsersAsync(QuickSaveRequestDto request)
    {
        if (request?.Rows == null || !request.Rows.Any())
        {
            throw new AppServiceException(400, "请求数据无效或为空！");
        }

        // 获取需要更新的用户ID列表
        List<string> userIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
        List<ApplicationUser> usersToUpdate = await GetUsersByIdsAsync(userIdsToUpdate);
        if (usersToUpdate.Count != userIdsToUpdate.Count)
        {
            throw new AppServiceException(400, "部分用户未找到!");
        }

        // 3. 执行批量更新：更新 `rowsDiff` 中的变化字段
        foreach (UserDiffDto rowDiff in request.RowsDiff)
        {
            ApplicationUser user = usersToUpdate.FirstOrDefault(u => u.Id == rowDiff.Id);
            if (user != null)
            {
                // 更新变化字段（仅更新在 rowsDiff 中的字段）
                if (rowDiff.IsActive.HasValue)
                {
                    user.IsActive = rowDiff.IsActive.Value;
                }

                if (rowDiff.LockoutEnabled.HasValue)
                {
                    user.LockoutEnabled = rowDiff.LockoutEnabled.Value;
                }

                // 可以根据需求增加更多字段的更新
            }
        }

        // 4. 保存更新结果
        int updateResult = await _userRepository.SaveChangesAsync();
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
        IQueryable<ApplicationUser> query = _userManager.Users
            .Where(u => u.CreationTime >= startDate.Date && u.CreationTime <= endDate.Date);

        // 按天统计用户注册数量
        var dailyGrowth = await query
            .GroupBy(u => u.CreationTime.Date)
            .Select(g => new { Date = g.Key, UserCount = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync();

        // 返回前端所需格式
        List<UserGrowthDto> result = dailyGrowth.Select(g => new UserGrowthDto
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
        IQueryable<ApplicationUser> query = _userManager.Users
            .Where(u => u.LastLoginTime >= startDate && u.LastLoginTime <= endDate);

        // 按天统计活跃用户数量
        var dailyActiveUsers = await query
            .GroupBy(u => u.LastLoginTime.Value.Date)
            .Select(g => new { Date = g.Key, ActiveUserCount = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync();

        // 返回前端所需格式
        List<ActiveUserDto> result = dailyActiveUsers.Select(g => new ActiveUserDto
        {
            Date = g.Date,
            ActiveUserCount = g.ActiveUserCount
        }).ToList();

        return result;
    }
    #endregion

    public async Task<int> BatchImportUsersAsync(List<UserBatchImportItemDto> importDtos)
    {
        // 数据不能为空
        if (importDtos == null || !importDtos.Any())
        {
            throw new AppServiceException(400, "导入数据不能为空！");
        }

        // 校验导入数据格式
        List<UserBatchImportItemDto> invalidDtos = importDtos.Where(dto =>
            string.IsNullOrEmpty(dto.UserName) ||
            string.IsNullOrEmpty(dto.Email) ||
            string.IsNullOrEmpty(dto.Name) ||  // 姓名为必填
            dto.UserName.Length > 100 ||
            dto.Email.Length > 256 ||
            (dto.PhoneNumber != null && dto.PhoneNumber.Length > 20) ||
            (dto.Name != null && dto.Name.Length > 20) ||
            (dto.IdNo != null && dto.IdNo.Length > 18)
        ).ToList();

        if (invalidDtos.Any())
        {
            throw new AppServiceException(400, $"以下用户数据格式错误: {string.Join(", ", invalidDtos.Select(dto => dto.UserName))}！");
        }

        // 去重处理：确保用户名、邮箱和手机号唯一
        List<UserBatchImportItemDto> distinctDtos = importDtos
            .GroupBy(dto => new { dto.UserName, dto.Email, dto.PhoneNumber })
            .Select(group => group.First())
            .ToList();

        // 检查数据库中是否已有重复的用户名、邮箱或手机号
        List<ApplicationUser> existingUsers = await _userManager.Users
            .Where(u => distinctDtos.Select(dto => dto.UserName).Contains(u.UserName) ||
                       distinctDtos.Select(dto => dto.Email).Contains(u.Email) ||
                       distinctDtos.Where(dto => !string.IsNullOrEmpty(dto.PhoneNumber))
                                 .Select(dto => dto.PhoneNumber)
                                 .Contains(u.PhoneNumber))
            .ToListAsync();

        List<UserBatchImportItemDto> duplicateUsers = distinctDtos.Where(dto =>
            existingUsers.Any(user =>
                user.UserName.Equals(dto.UserName, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber.Equals(user.PhoneNumber))
            )).ToList();

        if (duplicateUsers.Any())
        {
            throw new AppServiceException(400, $"以下用户名、邮箱或手机号已存在: {string.Join(", ", duplicateUsers.Select(dto => dto.UserName))}！");
        }

        // 批量创建用户
        try
        {
            foreach (UserBatchImportItemDto dto in distinctDtos)
            {
                ApplicationUser user = new()
                {
                    UserName = dto.UserName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Name = dto.Name,
                    IdNo = dto.IdNo,
                    Gender = dto.Gender,
                    IsActive = true,
                    EmailConfirmed = true, // 默认确认邮箱
                    PhoneNumberConfirmed = !string.IsNullOrEmpty(dto.PhoneNumber), // 如果提供了手机号，则确认
                    LockoutEnabled = true, // 启用锁定功能
                    CreationTime = DateTime.Now
                };

                // 生成随机密码
                string password = PasswordGenerator.GenerateRandomPassword(12);
                IdentityResult result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    _logger.LogError($"创建用户 {dto.UserName} 失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    throw new AppServiceException(500, $"创建用户 {dto.UserName} 失败！");
                }
            }

            _logger.LogInformation($"成功批量导入了 {distinctDtos.Count} 个用户！");
            return distinctDtos.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError($"批量导入用户数据失败: {ex.Message}");
            throw new AppServiceException(500, "批量导入用户时发生错误，请稍后重试！");
        }
    }

    public async Task<(int SuccessCount, List<string> FailedUserNames)> BatchDeleteUsersAsync(List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            throw new AppServiceException(400, "请选择要删除的用户！");
        }

        // 去重并验证ID格式
        userIds = userIds.Distinct().Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        if (!userIds.Any())
        {
            throw new AppServiceException(400, "无有效的用户ID！");
        }

        // 获取要删除的用户
        List<ApplicationUser> usersToDelete = await GetUsersByIdsAsync(userIds);

        if (!usersToDelete.Any())
        {
            throw new AppServiceException(400, "未找到指定的用户！");
        }

        // 验证是否包含当前用户
        string currentUserId = _userManager.GetUserId(_httpContextAccessor.HttpContext?.User);
        if (usersToDelete.Any(u => u.Id == currentUserId))
        {
            throw new AppServiceException(400, "不能删除当前登录用户！");
        }

        // 检查是否包含管理员用户
        List<string> adminUsers = [];
        foreach (ApplicationUser user in usersToDelete)
        {
            if (await _userManager.IsInRoleAsync(user, "Administrator"))
            {
                adminUsers.Add(user.UserName);
            }
        }

        if (adminUsers.Any())
        {
            throw new AppServiceException(400, $"以下管理员用户不能被删除: {string.Join(", ", adminUsers)}");
        }

        List<string> failUsers = [];
        foreach (ApplicationUser user in usersToDelete)
        {
            IdentityResult result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                failUsers.Add(user.UserName);
            }
        }

        _logger.LogInformation($"批量删除用户完成。成功删除 {usersToDelete.Count - failUsers.Count} 个用户");
        return (usersToDelete.Count - failUsers.Count, failUsers);
    }
}