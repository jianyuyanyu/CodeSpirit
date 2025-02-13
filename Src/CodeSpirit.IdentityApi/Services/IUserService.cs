using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Controllers.Dtos.User;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService: IScopedDependency
    {
        /// <summary>
        /// 分配角色给用户
        /// </summary>
        Task<IdentityResult> AssignRolesAsync(long id, List<string> roles);

        /// <summary>
        /// 批量删除用户
        /// </summary>
        Task<(int SuccessCount, List<string> FailedUserNames)> BatchDeleteUsersAsync(List<long> userIds);

        /// <summary>
        /// 批量导入用户
        /// </summary>
        Task<int> BatchImportUsersAsync(List<UserBatchImportItemDto> importDtos);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<(IdentityResult, long?)> CreateUserAsync(CreateUserDto createUserDto);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<IdentityResult> DeleteUserAsync(long id);

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate);

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<UserDto> GetUserByIdAsync(long id);

        /// <summary>
        /// 获取用户增长数据
        /// </summary>
        Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate);

        /// <summary>
        /// 获取用户列表
        /// </summary>
        Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto);

        /// <summary>
        /// 根据ID列表获取用户
        /// </summary>
        Task<List<ApplicationUser>> GetUsersByIdsAsync(List<long> ids);

        /// <summary>
        /// 快速保存用户
        /// </summary>
        Task QuickSaveUsersAsync(QuickSaveRequestDto request);

        /// <summary>
        /// 移除用户角色
        /// </summary>
        Task<IdentityResult> RemoveRolesAsync(long id, List<string> roles);

        /// <summary>
        /// 重置随机密码
        /// </summary>
        Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(long id);

        /// <summary>
        /// 设置用户活动状态
        /// </summary>
        Task<IdentityResult> SetActiveStatusAsync(long id, bool isActive);

        /// <summary>
        /// 解锁用户
        /// </summary>
        Task<IdentityResult> UnlockUserAsync(long id);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<IdentityResult> UpdateUserAsync(long id, UpdateUserDto updateUserDto);
    }
}