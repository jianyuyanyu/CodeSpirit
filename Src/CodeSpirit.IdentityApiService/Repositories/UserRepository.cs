using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Utilities;
using CodeSpirit.Shared;
using CodeSpirit.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Repositories
{
    public partial class UserRepository : Repository<ApplicationUser>, IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IMapper mapper) // 注入 IMapper
            : base(context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public async Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto)
        {
            var query = _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            query = ApplyFilters(query, queryDto);
            query = ApplySorting(query, queryDto);

            var totalCount = await query.CountAsync();
            var users = await query.ApplyPaging(queryDto.Page, queryDto.PerPage).ToListAsync();

            // 使用 AutoMapper 进行映射
            var userDtos = _mapper.Map<List<UserDto>>(users);

            return new ListData<UserDto>(userDtos, totalCount);
        }

        public async Task<UserDto> GetUserByIdAsync(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<(IdentityResult Result, string UserId)> CreateUserAsync(CreateUserDto createUserDto)
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

        public async Task<IdentityResult> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

            _context.SoftDelete(user);
            await _context.SaveChangesAsync();
            return IdentityResult.Success;
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

        public async Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                throw new AppServiceException(400, "账户不存在或已被禁用，请启用后再试！");

            var newPassword = PasswordGenerator.GenerateRandomPassword(12);
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            return resetResult.Succeeded ? (true, newPassword) : (false, null);
        }

        public async Task<IdentityResult> UnlockUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "用户不存在！" });

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.UtcNow)
                return IdentityResult.Failed(new IdentityError { Description = "该用户未被锁定，无需解锁。" });

            var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!setLockoutResult.Succeeded)
                return setLockoutResult;

            user.AccessFailedCount = 0;
            var updateResult = await _userManager.UpdateAsync(user);
            return updateResult.Succeeded ? IdentityResult.Success : updateResult;
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

        #region 私有辅助方法

        private IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query, UserQueryDto queryDto)
        {
            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                var searchLower = queryDto.Keywords.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    u.IdNo.Contains(queryDto.Keywords) ||
                    u.UserName.ToLower().Contains(searchLower));
            }

            if (queryDto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == queryDto.IsActive.Value);
            }

            if (queryDto.Gender.HasValue)
            {
                query = query.Where(u => u.Gender == queryDto.Gender.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Role))
            {
                var roleName = queryDto.Role.Trim();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
            }

            if (queryDto.LastLoginTime != null && queryDto.LastLoginTime.Length == 2)
            {
                var startDate = queryDto.LastLoginTime[0];
                query = query.Where(u => u.LastLoginTime >= startDate);

                var endDate = queryDto.LastLoginTime[1];
                query = query.Where(u => u.LastLoginTime <= endDate);
            }
            return query;
        }

        private IQueryable<ApplicationUser> ApplySorting(IQueryable<ApplicationUser> query, UserQueryDto queryDto)
        {
            if (!string.IsNullOrWhiteSpace(queryDto.OrderBy))
            {
                var allowedSortFields = new List<string>
                {
                    "name",
                    "email",
                    "lastlogintime",
                    "username",
                    "gender",
                    "phonenumber"
                    // 其他允许排序的字段可以在这里添加
                };

                if (allowedSortFields.Contains(queryDto.OrderBy.ToLower()))
                {
                    query = query.ApplySorting(queryDto.OrderBy, queryDto.OrderDir, allowedSortFields);
                }
                else
                {
                    // 默认排序
                    query = query.OrderBy(u => u.Id);
                }
            }
            else
            {
                // 默认排序
                query = query.OrderBy(u => u.Id);
            }

            return query;
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

        #endregion
    }
}
