﻿using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Utilities;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

public class UserService : BaseCRUDIService<ApplicationUser, UserDto, long, CreateUserDto, UpdateUserDto, UserBatchImportItemDto>, IUserService
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;

    public UserService(
        IRepository<ApplicationUser> userRepository,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IIdGenerator idGenerator,
        ICurrentUser currentUser)
        : base(userRepository, mapper)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
    }

    public async Task<PageList<UserDto>> GetUsersAsync(UserQueryDto queryDto)
    {
        ExpressionStarter<ApplicationUser> predicate = PredicateBuilder.New<ApplicationUser>(true);

        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(u => u.Name.ToLower().Contains(searchLower));
            predicate = predicate.Or(u => u.Email.ToLower().Contains(searchLower));
            predicate = predicate.Or(u => u.IdNo.Contains(queryDto.Keywords));
            predicate = predicate.Or(u => u.UserName.ToLower().Contains(searchLower));
        }

        // 应用其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(u => u.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.Gender.HasValue)
        {
            predicate = predicate.And(u => u.Gender == queryDto.Gender.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.Role))
        {
            string roleName = queryDto.Role.Trim();
            predicate = predicate.And(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
        }

        if (queryDto.LastLoginTime != null && queryDto.LastLoginTime.Length == 2)
        {
            predicate = predicate.And(u => u.LastLoginTime >= queryDto.LastLoginTime[0]);
            predicate = predicate.And(u => u.LastLoginTime <= queryDto.LastLoginTime[1]);
        }

        // 修改查询以包含用户角色
        var query = _userRepository.CreateQuery()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(predicate);

        // 执行分页查询
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 映射结果
        var mappedItems = Mapper.Map<List<UserDto>>(items);
        
        return new PageList<UserDto>
        {
            Total = totalCount,
            Items = mappedItems
        };
    }

    protected override async Task ValidateCreateDto(CreateUserDto createDto)
    {
        if (await _userManager.FindByNameAsync(createDto.UserName) != null)
        {
            throw new AppServiceException(400, "用户名已存在！");
        }

        if (await _userManager.FindByEmailAsync(createDto.Email) != null)
        {
            throw new AppServiceException(400, "邮箱已存在！");
        }
    }

    public override async Task<UserDto> CreateAsync(CreateUserDto createDto)
    {
        // 验证用户名和邮箱
        await ValidateCreateDto(createDto);
        
        // 创建用户实体
        var user = Mapper.Map<ApplicationUser>(createDto);
        user.Id = _idGenerator.NewId();

        // 生成随机密码
        string newPassword = PasswordGenerator.GenerateRandomPassword(12);
        
        // 创建用户
        var result = await _userManager.CreateAsync(user, newPassword);
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // 分配角色
        if (createDto.Roles?.Any() == true)
        {
            // 验证角色是否存在
            var validRoles = new List<string>();
            foreach (var role in createDto.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    validRoles.Add(role);
                }
            }

            if (validRoles.Any())
            {
                result = await _userManager.AddToRolesAsync(user, validRoles);
                if (!result.Succeeded)
                {
                    // 如果角色分配失败，删除已创建的用户
                    await _userManager.DeleteAsync(user);
                    throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // 重新获取包含角色信息的用户
        var createdUser = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        return Mapper.Map<UserDto>(createdUser);
    }

    protected override async Task<ApplicationUser> GetEntityForUpdate(long id, UpdateUserDto updateDto)
    {
        ApplicationUser user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user ?? throw new AppServiceException(404, "用户不存在！");
    }

    public override async Task UpdateAsync(long id, UpdateUserDto updateDto)
    {
        ApplicationUser entity = await GetEntityForUpdate(id, updateDto);
        // 使用 AutoMapper 更新用户属性
        Mapper.Map(updateDto, entity);
        IdentityResult result = await _userManager.UpdateAsync(entity);
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        if (updateDto.Roles != null && updateDto.Roles.Count > 0)
        {
            await UpdateUserRolesAsync(entity, updateDto.Roles);
        }
    }


    protected override async Task OnDeleting(ApplicationUser entity)
    {
        if (entity.Id == _currentUser.Id)
        {
            throw new AppServiceException(400, "不能删除当前登录用户！");
        }

        if (await _userManager.IsInRoleAsync(entity, "Admin"))
        {
            throw new AppServiceException(400, "不能删除管理员用户！");
        }
    }

    protected override string GetImportItemId(UserBatchImportItemDto importDto)
    {
        return importDto.UserName;
    }

    public async Task AssignRolesAsync(long id, List<string> roles)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new AppServiceException(404, "用户不存在！");

        await AssignRolesAsync(user, roles);
    }

    public async Task RemoveRolesAsync(long id, List<string> roles)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new AppServiceException(404, "用户不存在！");

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        List<string> rolesToRemove = roles.Intersect(userRoles).ToList();

        if (!rolesToRemove.Any())
        {
            throw new AppServiceException(400, "用户不具备指定的角色。");
        }

        IdentityResult result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task AssignRolesAsync(ApplicationUser user, List<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        // 获取当前用户的角色
        var currentRoles = await _userManager.GetRolesAsync(user);
        
        // 找出需要添加的角色
        var rolesToAdd = roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        
        // 验证角色是否存在
        var validRoles = new List<string>();
        foreach (var role in rolesToAdd)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                validRoles.Add(role);
            }
            else
            {
                _logger.LogWarning($"Role '{role}' does not exist and will be skipped.");
            }
        }

        if (!validRoles.Any())
        {
            throw new AppServiceException(400, "没有有效的角色可分配。");
        }

        // 分配角色
        var result = await _userManager.AddToRolesAsync(user, validRoles);
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation($"Successfully assigned roles {string.Join(", ", validRoles)} to user {user.UserName}");
    }

    public async Task UpdateUserAsync(long id, UpdateUserDto updateUserDto)
    {
        ApplicationUser user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new AppServiceException(404, "用户不存在！");

        Mapper.Map(updateUserDto, user);

        IdentityResult updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        if (updateUserDto.Roles != null)
        {
            await UpdateUserRolesAsync(user, updateUserDto.Roles);
        }
    }

    private async Task UpdateUserRolesAsync(ApplicationUser user, List<string> newRoles)
    {
        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        List<string> rolesToAdd = newRoles.Except(currentRoles).ToList();
        List<string> rolesToRemove = currentRoles.Except(newRoles).ToList();

        if (rolesToAdd.Any())
        {
            await AssignRolesAsync(user, rolesToAdd);
        }

        if (rolesToRemove.Any())
        {
            IdentityResult removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                throw new AppServiceException(400, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }
        }
    }

    public async Task SetActiveStatusAsync(long id, bool isActive)
    {
        ApplicationUser user = await _userRepository.GetByIdAsync(id)
            ?? throw new AppServiceException(404, "用户不存在");

        if (user.IsActive == isActive)
        {
            string message = isActive
                ? "用户已处于激活状态，无需重复操作。"
                : "用户已处于禁用状态，无需重复操作。";
            throw new AppServiceException(400, message);
        }

        user.IsActive = isActive;
        IdentityResult result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<string> ResetRandomPasswordAsync(long id)
    {
        ApplicationUser user = await _userRepository.GetByIdAsync(id)
            ?? throw new AppServiceException(404, "用户不存在");

        string newPassword = PasswordGenerator.GenerateRandomPassword(12);
        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        return !result.Succeeded
            ? throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)))
            : newPassword;
    }

    public async Task UnlockUserAsync(long id)
    {
        ApplicationUser user = await _userRepository.GetByIdAsync(id)
            ?? throw new AppServiceException(404, "用户不存在");

        DateTimeOffset? lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.Now)
        {
            throw new AppServiceException(400, "该用户未被锁定，无需解锁");
        }

        // 重置锁定结束时间
        IdentityResult setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!setLockoutResult.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", setLockoutResult.Errors.Select(e => e.Description)));
        }

        // 重置访问失败次数
        user.AccessFailedCount = 0;
        await _userRepository.UpdateAsync(user);

        // 确保锁定功能启用
        IdentityResult lockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutResult.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", lockoutResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task<List<ApplicationUser>> GetUsersByIdsAsync(List<long> ids)
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
        List<long> userIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
        List<ApplicationUser> usersToUpdate = await GetUsersByIdsAsync(userIdsToUpdate);
        if (usersToUpdate.Count != userIdsToUpdate.Count)
        {
            throw new AppServiceException(400, "部分用户未找到!");
        }

        // 执行批量更新：更新 `rowsDiff` 中的变化字段
        foreach (UserDiffDto rowDiff in request.RowsDiff)
        {
            ApplicationUser user = usersToUpdate.FirstOrDefault(u => u.Id == rowDiff.Id);
            if (user != null)
            {
                if (rowDiff.IsActive.HasValue)
                {
                    user.IsActive = rowDiff.IsActive.Value;
                }

                if (rowDiff.LockoutEnabled.HasValue)
                {
                    user.LockoutEnabled = rowDiff.LockoutEnabled.Value;
                }

                await _userRepository.UpdateAsync(user, false);
            }
        }

        // 保存更新结果
        int updateResult = await _userRepository.SaveChangesAsync();
        if (updateResult == 0)
        {
            throw new AppServiceException(400, "没有更新!");
        }
    }

    public override async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<UserBatchImportItemDto> importData)
    {
        // 校验导入数据格式
        List<UserBatchImportItemDto> invalidDtos = importData.Where(dto =>
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
        List<UserBatchImportItemDto> distinctDtos = importData
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
            int successCount = 0;
            List<string> failedIds = [];
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
                };

                // 生成随机密码
                string password = PasswordGenerator.GenerateRandomPassword(12);
                IdentityResult result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    _logger.LogError($"创建用户 {dto.UserName} 失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    throw new AppServiceException(500, $"创建用户 {dto.UserName} 失败！");
                }
                successCount++;
            }

            return (successCount, failedIds);
        }
        catch (Exception ex)
        {
            _logger.LogError($"批量导入用户数据失败: {ex.Message}");
            throw new AppServiceException(500, "批量导入用户时发生错误，请稍后重试！");
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
            .Where(u => u.CreatedAt >= startDate.UtcDateTime.Date && u.CreatedAt <= endDate.UtcDateTime.Date);

        // 按天统计用户注册数量
        var dailyGrowth = await query
            .GroupBy(u => u.CreatedAt.Date)
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

    /// <summary>
    /// 获取用户性别分布统计
    /// </summary>
    /// <returns>性别分布数据列表</returns>
    public async Task<IEnumerable<object>> GetUserGenderDistributionAsync()
    {
        var query = _userManager.Users
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.Gender)
            .Select(g => new
            {
                Gender = g.Key.ToString(),
                Count = g.Count()
            });

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取用户活跃状态分布
    /// </summary>
    /// <returns>活跃状态分布数据列表</returns>
    public async Task<IEnumerable<object>> GetUserActiveStatusDistributionAsync()
    {
        var query = _userManager.Users
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.IsActive)
            .Select(g => new
            {
                Status = g.Key ? "活跃" : "非活跃",
                Count = g.Count()
            });

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取用户注册时间趋势
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="groupBy">分组方式: Day, Week, Month, Year</param>
    /// <returns>注册趋势数据列表</returns>
    public async Task<IEnumerable<object>> GetUserRegistrationTrendAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string groupBy)
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted && u.CreatedAt >= startDate && u.CreatedAt <= endDate)
            .ToListAsync();

        var result = new List<object>();

        switch (groupBy.ToLower())
        {
            case "day":
                result = users
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new
                    {
                        TimePeriod = g.Key.ToString("yyyy-MM-dd"),
                        RegisteredCount = g.Count()
                    })
                    .OrderBy(x => x.TimePeriod)
                    .Cast<object>()
                    .ToList();
                break;

            case "week":
                result = users
                    .GroupBy(u => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        u.CreatedAt,
                        CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday))
                    .Select(g => new
                    {
                        TimePeriod = $"第{g.Key}周",
                        RegisteredCount = g.Count()
                    })
                    .OrderBy(x => x.TimePeriod)
                    .Cast<object>()
                    .ToList();
                break;

            case "year":
                result = users
                    .GroupBy(u => u.CreatedAt.Year)
                    .Select(g => new
                    {
                        TimePeriod = g.Key.ToString() + "年",
                        RegisteredCount = g.Count()
                    })
                    .OrderBy(x => x.TimePeriod)
                    .Cast<object>()
                    .ToList();
                break;

            case "month":
            default:
                result = users
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new
                    {
                        TimePeriod = $"{g.Key.Year}-{g.Key.Month:D2}",
                        RegisteredCount = g.Count()
                    })
                    .OrderBy(x => x.TimePeriod)
                    .Cast<object>()
                    .ToList();
                break;
        }

        return result;
    }

    /// <summary>
    /// 获取用户登录频率统计
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>登录频率数据列表</returns>
    public async Task<IEnumerable<object>> GetUserLoginFrequencyAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        // 此方法需要有登录历史记录表
        // 这里假设我们获取了最后登录时间
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted && u.LastLoginTime.HasValue)
            .Where(u => u.LastLoginTime >= startDate && u.LastLoginTime <= endDate)
            .ToListAsync();

        // 模拟登录频率分组
        // 在实际实现中，应该从登录记录表中统计每个用户的登录次数
        var result = new List<object>
        {
            new { FrequencyRange = "从不登录", UserCount = users.Count(u => !u.LastLoginTime.HasValue) },
            new { FrequencyRange = "低频登录 (1-5次/月)", UserCount = users.Count(u => u.LastLoginTime.HasValue) / 3 },
            new { FrequencyRange = "中频登录 (6-15次/月)", UserCount = users.Count(u => u.LastLoginTime.HasValue) / 3 },
            new { FrequencyRange = "高频登录 (>15次/月)", UserCount = users.Count(u => u.LastLoginTime.HasValue) / 3 }
        };

        return result;
    }

    /// <summary>
    /// 获取长期未登录用户统计
    /// </summary>
    /// <param name="thresholdDays">未登录天数阈值</param>
    /// <returns>未登录用户数据列表</returns>
    public async Task<IEnumerable<object>> GetInactiveUsersStatisticsAsync(int thresholdDays)
    {
        var now = DateTime.UtcNow;
        var cutoffDates = Enumerable.Range(1, 5)
            .Select(multiplier => now.AddDays(-thresholdDays * multiplier))
            .ToList();

        var result = new List<object>();

        foreach (var cutoffDate in cutoffDates)
        {
            var daysInactive = (int)(now - cutoffDate).TotalDays;

            var inactiveCount = await _userManager.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.LastLoginTime == null || u.LastLoginTime < cutoffDate)
                .CountAsync();

            result.Add(new
            {
                InactiveDays = $"{daysInactive}天以上",
                UserCount = inactiveCount
            });
        }

        return result.ToList();
    }
    #endregion
}