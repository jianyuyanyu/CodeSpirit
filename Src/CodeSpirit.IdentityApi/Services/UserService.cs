using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Utilities;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.EventBus.Publishers;
using CodeSpirit.Shared.EventBus.Events;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using CodeSpirit.Core.Extensions;

public class UserService : BaseCRUDIService<ApplicationUser, UserDto, long, CreateUserDto, UpdateUserDto, UserBatchImportItemDto>, IUserService
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UserService> _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly FileReferenceEventPublisher _fileReferenceEventPublisher;

    public UserService(
        IRepository<ApplicationUser> userRepository,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UserService> logger,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        ApplicationDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        FileReferenceEventPublisher fileReferenceEventPublisher)
        : base(userRepository, mapper)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _fileReferenceEventPublisher = fileReferenceEventPublisher ?? throw new ArgumentNullException(nameof(fileReferenceEventPublisher));
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
        // 使用数据库上下文直接查询，确保多租户过滤器正常工作
        // 避免使用UserManager，因为它可能不会应用EF Core的查询过滤器
        
        var existingUserByName = await _dbContext.Users
            .Where(u => u.UserName == createDto.UserName)
            .FirstOrDefaultAsync();
        if (existingUserByName != null)
        {
            throw new AppServiceException(400, "用户名已存在！");
        }

        if (!string.IsNullOrEmpty(createDto.Email))
        {
            var existingUserByEmail = await _dbContext.Users
                .Where(u => u.Email == createDto.Email)
                .FirstOrDefaultAsync();
            if (existingUserByEmail != null)
            {
                throw new AppServiceException(400, "邮箱已存在！");
            }
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

        var userDto = Mapper.Map<UserDto>(createdUser);

        // 发布用户创建文件引用事件
        try
        {
            var fileReferences = new List<FileReferenceInfo>();
            var avatarFileId = FileReferenceEventPublisher.ExtractFileIdFromUrl(createDto.AvatarUrl);
            if (avatarFileId.HasValue || !string.IsNullOrEmpty(createDto.AvatarUrl))
            {
                fileReferences.Add(FileReferenceEventPublisher.CreateFileReference(
                    avatarFileId, createDto.AvatarUrl ?? string.Empty, "Avatar", "用户头像", isPrimary: true));
            }
            
            await _fileReferenceEventPublisher.PublishFileReferenceEventAsync(
                typeof(ApplicationUser).FullName,
                user.Id.ToString(),
                user.Name,
                FileReferenceOperationType.Create,
                fileReferences,
                _currentUser?.Id,
                _currentUser?.UserName ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发布用户创建文件引用事件失败: 用户ID={UserId}", user.Id);
            // 不抛出异常，避免影响用户创建流程
        }

        return userDto;
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

        // 发布用户更新文件引用事件
        try
        {
            var fileReferences = new List<FileReferenceInfo>();
            var avatarFileId = FileReferenceEventPublisher.ExtractFileIdFromUrl(updateDto.AvatarUrl);
            if (avatarFileId.HasValue || !string.IsNullOrEmpty(updateDto.AvatarUrl))
            {
                fileReferences.Add(FileReferenceEventPublisher.CreateFileReference(
                    avatarFileId, updateDto.AvatarUrl ?? string.Empty, "Avatar", "用户头像", isPrimary: true));
            }
            
            await _fileReferenceEventPublisher.PublishFileReferenceEventAsync(
                typeof(ApplicationUser).FullName,
                entity.Id.ToString(),
                entity.Name,
                FileReferenceOperationType.Update,
                fileReferences,
                _currentUser?.Id,
                _currentUser?.UserName ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发布用户更新文件引用事件失败: 用户ID={UserId}", entity.Id);
            // 不抛出异常，避免影响用户更新流程
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

        // 发布用户删除文件引用事件
        try
        {
            await _fileReferenceEventPublisher.PublishFileReferenceEventAsync(
                typeof(ApplicationUser).FullName,
                entity.Id.ToString(),
                entity.Name,
                FileReferenceOperationType.Delete,
                new List<FileReferenceInfo>(), // 删除时传入空列表
                _currentUser?.Id,
                _currentUser?.UserName ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发布用户删除文件引用事件失败: 用户ID={UserId}", entity.Id);
            // 不抛出异常，避免影响用户删除流程
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
        try
        {
            IQueryable<ApplicationUser> query = _userManager.Users
                .Where(u => u.LastLoginTime >= startDate && u.LastLoginTime <= endDate);

            // 按天统计活跃用户数量
            var dailyActiveUsers = await query
                .GroupBy(u => u.LastLoginTime.Value.Date)
                .Select(g => new { Date = g.Key, ActiveUserCount = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            _logger.LogInformation("查询到的活跃用户数据条数: {Count}", dailyActiveUsers.Count);

            // 返回前端所需格式
            List<ActiveUserDto> result = dailyActiveUsers.Select(g => new ActiveUserDto
            {
                Date = g.Date,
                ActiveUserCount = g.ActiveUserCount
            }).ToList();

            // 如果没有数据，添加一条默认数据以避免空集合异常
            if (result.Count == 0)
            {
                _logger.LogWarning("活跃用户数据为空，添加默认数据");
                result.Add(new ActiveUserDto 
                { 
                    Date = DateTime.Now.Date, 
                    ActiveUserCount = 0 
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃用户统计时发生错误");
            // 发生异常时返回含有一条默认数据的列表，而不是空列表
            return new List<ActiveUserDto> 
            { 
                new ActiveUserDto 
                { 
                    Date = DateTime.Now.Date, 
                    ActiveUserCount = 0 
                } 
            };
        }
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
                Gender = g.Key.GetDisplayName(),
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
        try
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

            _logger.LogInformation("查询到的未登录用户数据条数: {Count}", result.Count);

            // 确保结果非空
            if (result.Count == 0)
            {
                _logger.LogWarning("未登录用户数据为空，添加默认数据");
                result.Add(new
                {
                    InactiveDays = $"{thresholdDays}天以上",
                    UserCount = 0
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取未登录用户统计时发生错误");
            return new List<object>
            {
                new
                {
                    InactiveDays = $"{thresholdDays}天以上",
                    UserCount = 0
                }
            };
        }
    }
    #endregion

    /// <summary>
    /// 高级用户创建方法，支持指定密码及创建者等信息
    /// </summary>
    /// <param name="createDto">用户创建数据传输对象</param>
    /// <param name="password">指定的用户密码，如为null则自动生成随机密码</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="sendPasswordEmail">是否发送密码邮件</param>
    /// <param name="userId"> userId不为空时,将userId作为新创建的用户的Id,否则将自动生成Id</param>
    /// <param name="skipValidation">是否跳过验证（用于事件处理等场景）</param>
    /// <returns>创建的用户数据传输对象</returns>
    public async Task<UserDto> CreateAdvancedUserAsync(
        CreateUserDto createDto,
        string password = null,
        long? creatorId = null,
        long? userId = null,
        bool skipValidation = false)
    {
        // 验证用户名和邮箱（除非明确跳过）
        if (!skipValidation)
        {
            await ValidateCreateDto(createDto);
        }
        
        // 创建用户实体
        var user = Mapper.Map<ApplicationUser>(createDto);
        user.Id = userId.HasValue && userId.Value != default ? userId.Value : _idGenerator.NewId();
        
        // 记录创建者信息
        if (creatorId.HasValue)
        {
            user.CreatedBy = creatorId.Value;
        }
        else if (_currentUser != null && _currentUser.Id > 0)
        {
            // 如果没有指定创建者ID，使用当前用户ID
            user.CreatedBy = _currentUser.Id.Value;
        }
        
        // 使用指定密码或生成随机密码
        string newPassword = password ?? PasswordGenerator.GenerateRandomPassword(12);
        
        IdentityResult result;
        
        if (skipValidation)
        {
            // 跨租户事件处理：直接使用 DbContext 创建用户，绕过 UserManager 的唯一性验证
            // 设置必要的用户属性
            user.NormalizedUserName = createDto.UserName?.ToUpper();
            user.NormalizedEmail = createDto.Email?.ToUpper();
            user.EmailConfirmed = true;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            
            // 设置审计信息
            user.CreatedAt = DateTime.UtcNow;
            user.IsDeleted = false;
            
            // 确保租户ID设置正确
            // 优先使用CurrentUser的租户ID（在事件上下文中会是EventCurrentUser）
            if (string.IsNullOrEmpty(user.TenantId))
            {
                if (!string.IsNullOrEmpty(_currentUser?.TenantId))
                {
                    user.TenantId = _currentUser.TenantId;
                    _logger.LogDebug("从CurrentUser设置租户ID: {TenantId}", user.TenantId);
                }
                else
                {
                    // 如果CurrentUser中没有租户ID，使用默认租户ID
                    user.TenantId = "default";
                    _logger.LogWarning("无法获取租户ID，使用默认租户ID: default");
                }
            }
            else
            {
                _logger.LogDebug("用户实体已有租户ID: {TenantId}", user.TenantId);
            }
            
            // 直接添加到数据库
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            
            // 模拟成功的 IdentityResult
            result = IdentityResult.Success;
        }
        else
        {
            // 正常流程：使用 UserManager 创建用户
            result = await _userManager.CreateAsync(user, newPassword);
            if (!result.Succeeded)
            {
                throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
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

    /// <summary>
    /// 根据用户名查询用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户信息</returns>
    public async Task<UserDto> GetUserByUserNameAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new AppServiceException(400, "用户名不能为空！");
        }

        var user = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName);

        if (user == null)
        {
            return null;
        }

        return Mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// 根据手机号查询用户
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>用户信息</returns>
    public async Task<UserDto> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new AppServiceException(400, "手机号不能为空！");
        }
        
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        
        if (user == null)
        {
            return null;
        }
        
        return Mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// 根据身份证号码查询用户
    /// </summary>
    /// <param name="idNo">身份证号码</param>
    /// <returns>用户信息</returns>
    public async Task<UserDto> GetUserByIdNoAsync(string idNo)
    {
        if (string.IsNullOrWhiteSpace(idNo))
        {
            throw new ArgumentException("身份证号码不能为空", nameof(idNo));
        }

        ApplicationUser? user = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.IdNo == idNo);

        if (user == null)
        {
            throw new AppServiceException(404, "用户不存在！");
        }

        return Mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// 根据用户ID获取用户信息（忽略所有过滤器）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息</returns>
    /// <remarks>
    /// 此方法会忽略租户过滤器和软删除过滤器，主要用于获取当前登录用户自己的信息
    /// </remarks>
    public async Task<UserDto> GetUserByIdIgnoreFiltersAsync(long userId)
    {
        try
        {
            // 直接使用DbContext查询，忽略所有全局过滤器（包括租户过滤器和软删除过滤器）
            var user = await _dbContext.Set<ApplicationUser>()
                .IgnoreQueryFilters() // 忽略所有全局过滤器
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            return Mapper.Map<UserDto>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户信息时发生异常: UserId={UserId}", userId);
            throw new AppServiceException(500, "查询用户信息失败");
        }
    }

    /// <summary>
    /// 获取用户角色（忽略多租户过滤器）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>角色名称列表</returns>
    /// <remarks>
    /// 此方法会忽略租户过滤器，确保系统平台和租户平台都能正确获取到用户角色
    /// </remarks>
    public async Task<List<string>> GetUserRolesIgnoreFiltersAsync(long userId)
    {
        try
        {
            // 直接使用DbContext查询，忽略所有全局过滤器（包括租户过滤器和软删除过滤器）
            var roles = await _dbContext.UserRoles
                .IgnoreQueryFilters() // 忽略所有全局过滤器
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles.IgnoreQueryFilters(), // 角色表也要忽略过滤器
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { UserRole = ur, Role = r })
                .Where(joined => !joined.Role.IsDeleted && joined.Role.IsActive) // 手动过滤无效角色
                .Select(joined => joined.Role.Name)
                .ToListAsync();

            _logger.LogDebug("为用户 {UserId} 获取到 {RoleCount} 个角色: {Roles}", 
                userId, roles.Count, string.Join(", ", roles));

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户角色时发生异常: UserId={UserId}", userId);
            return new List<string>();
        }
    }

    #region 系统平台专用方法实现

    /// <summary>
    /// 获取系统用户列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统用户分页列表</returns>
    public async Task<PageList<UserDto>> GetSystemUsersAsync(SystemUserQueryDto queryDto)
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

        // 应用用户名过滤
        if (!string.IsNullOrWhiteSpace(queryDto.UserName))
        {
            predicate = predicate.And(u => u.UserName.Contains(queryDto.UserName));
        }

        // 应用邮箱过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Email))
        {
            predicate = predicate.And(u => u.Email.Contains(queryDto.Email));
        }

        // 应用手机号过滤
        if (!string.IsNullOrWhiteSpace(queryDto.PhoneNumber))
        {
            predicate = predicate.And(u => u.PhoneNumber.Contains(queryDto.PhoneNumber));
        }

        // 应用激活状态过滤
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(u => u.IsActive == queryDto.IsActive.Value);
        }

        // 应用租户ID过滤
        if (!string.IsNullOrWhiteSpace(queryDto.TenantId))
        {
            predicate = predicate.And(u => u.TenantId == queryDto.TenantId);
        }

        // 应用创建时间过滤
        if (queryDto.CreatedAtStart.HasValue)
        {
            predicate = predicate.And(u => u.CreatedAt >= queryDto.CreatedAtStart.Value);
        }

        if (queryDto.CreatedAtEnd.HasValue)
        {
            predicate = predicate.And(u => u.CreatedAt <= queryDto.CreatedAtEnd.Value);
        }

        // 应用最后登录时间过滤
        if (queryDto.LastLoginStart.HasValue)
        {
            predicate = predicate.And(u => u.LastLoginTime >= queryDto.LastLoginStart.Value);
        }

        if (queryDto.LastLoginEnd.HasValue)
        {
            predicate = predicate.And(u => u.LastLoginTime <= queryDto.LastLoginEnd.Value);
        }

        // 创建查询（注意：这里不应用租户过滤，允许跨租户查询）
        var query = _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters() // 忽略多租户查询过滤器
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

    /// <summary>
    /// 获取按租户分组的用户统计信息
    /// </summary>
    /// <returns>租户用户统计列表</returns>
    public async Task<List<TenantUserStatisticsDto>> GetUsersByTenantStatisticsAsync()
    {
        var now = DateTimeOffset.Now;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);

        // 跨租户查询所有用户
        var users = await _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();

        // 按租户分组统计
        var statistics = users
            .GroupBy(u => u.TenantId ?? "default")
            .Select(g => new TenantUserStatisticsDto
            {
                TenantId = g.Key,
                TenantName = g.Key, // 这里可以后续从租户表中获取真实名称
                TenantDisplayName = g.Key,
                TotalUsers = g.Count(),
                ActiveUsers = g.Count(u => u.IsActive),
                InactiveUsers = g.Count(u => !u.IsActive),
                AdminUsers = g.Count(u => u.UserRoles.Any(ur => ur.Role.Name.Contains("Admin"))),
                NormalUsers = g.Count(u => !u.UserRoles.Any(ur => ur.Role.Name.Contains("Admin"))),
                NewUsersThisMonth = g.Count(u => u.CreatedAt >= startOfMonth),
                ActiveUsersThisMonth = g.Count(u => u.IsActive && u.LastLoginTime >= startOfMonth),
                LastActiveTime = g.Where(u => u.LastLoginTime.HasValue).Max(u => u.LastLoginTime),
                CreatedAt = g.Min(u => u.CreatedAt),
                GrowthRate = 0, // 待实现增长率计算
                ActivityRate = g.Any() ? (decimal)g.Count(u => u.IsActive) / g.Count() * 100 : 0
            })
            .ToList();

        return statistics;
    }

    /// <summary>
    /// 将用户转移到指定租户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetTenantId">目标租户ID</param>
    /// <returns>转移后的用户信息</returns>
    public async Task<UserDto> TransferUserToTenantAsync(long userId, string targetTenantId)
    {
        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            throw new ArgumentException("目标租户ID不能为空", nameof(targetTenantId));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AppServiceException(404, "用户不存在！");
        }

        // 更新用户的租户ID
        user.TenantId = targetTenantId;
        var result = await _userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            throw new AppServiceException(400, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // 重新获取包含角色信息的用户
        var updatedUser = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return Mapper.Map<UserDto>(updatedUser);
    }

    /// <summary>
    /// 获取跨租户用户增长趋势对比
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>各租户用户增长趋势数据</returns>
    public async Task<IEnumerable<object>> GetCrossTenantUserGrowthTrendAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var users = await _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters()
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
            .ToListAsync();

        var result = users
            .GroupBy(u => new { u.TenantId, Date = u.CreatedAt.Date })
            .Select(g => new
            {
                Date = g.Key.Date.ToString("yyyy-MM-dd"),
                TenantName = g.Key.TenantId ?? "default",
                UserCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.TenantName);

        return result;
    }

    /// <summary>
    /// 获取各租户用户活跃度排行
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>租户活跃度排行数据</returns>
    public async Task<IEnumerable<object>> GetTenantUserActivityRankingAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var users = await _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters()
            .ToListAsync();

        var result = users
            .GroupBy(u => u.TenantId ?? "default")
            .Select(g => new
            {
                TenantName = g.Key,
                TotalUsers = g.Count(),
                ActiveUsers = g.Count(u => u.IsActive && u.LastLoginTime >= startDate),
                ActivityScore = g.Any() ? (decimal)g.Count(u => u.IsActive && u.LastLoginTime >= startDate) / g.Count() * 100 : 0
            })
            .OrderByDescending(x => x.ActivityScore);

        return result;
    }

    /// <summary>
    /// 获取租户用户规模分布统计
    /// </summary>
    /// <returns>租户用户规模分布数据</returns>
    public async Task<IEnumerable<object>> GetTenantUserScaleDistributionAsync()
    {
        var users = await _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters()
            .ToListAsync();

        var tenantUserCounts = users
            .GroupBy(u => u.TenantId ?? "default")
            .Select(g => g.Count())
            .ToList();

        var scaleRanges = new[]
        {
            new { Range = "1-10人", Min = 1, Max = 10 },
            new { Range = "11-50人", Min = 11, Max = 50 },
            new { Range = "51-100人", Min = 51, Max = 100 },
            new { Range = "101-500人", Min = 101, Max = 500 },
            new { Range = "500人以上", Min = 501, Max = int.MaxValue }
        };

        var result = scaleRanges.Select(range => new
        {
            ScaleRange = range.Range,
            TenantCount = tenantUserCounts.Count(count => count >= range.Min && count <= range.Max)
        });

        return result;
    }

    /// <summary>
    /// 获取系统整体用户健康度分析
    /// </summary>
    /// <returns>用户健康度分析数据</returns>
    public async Task<object> GetSystemUserHealthAnalysisAsync()
    {
        var users = await _dbContext.Set<ApplicationUser>()
            .IgnoreQueryFilters()
            .ToListAsync();

        var now = DateTimeOffset.Now;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        var result = new
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            InactiveUsers = users.Count(u => !u.IsActive),
            RecentlyActiveUsers = users.Count(u => u.LastLoginTime >= sevenDaysAgo),
            LongInactiveUsers = users.Count(u => !u.LastLoginTime.HasValue || u.LastLoginTime < thirtyDaysAgo),
            NewUsersLast30Days = users.Count(u => u.CreatedAt >= thirtyDaysAgo),
            HealthScore = users.Any() ? (decimal)users.Count(u => u.IsActive && u.LastLoginTime >= sevenDaysAgo) / users.Count * 100 : 0,
            LastUpdated = now
        };

        return result;
    }

    /// <summary>
    /// 获取租户用户迁移历史统计
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>用户迁移历史统计数据</returns>
    public async Task<IEnumerable<object>> GetUserTransferHistoryStatisticsAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        // 注意：这里需要实际的用户迁移记录表才能提供准确的数据
        // 暂时返回模拟数据结构
        var result = new[]
        {
            new
            {
                Date = startDate.ToString("yyyy-MM-dd"),
                TransferCount = 0,
                SourceTenant = "模拟数据",
                TargetTenant = "模拟数据"
            }
        };

        return await Task.FromResult(result);
    }

    #endregion


}