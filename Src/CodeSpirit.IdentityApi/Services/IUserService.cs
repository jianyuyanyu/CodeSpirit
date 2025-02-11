using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 为用户分配角色
        /// </summary>
        Task<IdentityResult> AssignRolesAsync(string id, List<string> roles);

        /// <summary>
        /// 创建新用户
        /// </summary>
        Task<(IdentityResult, string)> CreateUserAsync(CreateUserDto createUserDto);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<IdentityResult> DeleteUserAsync(string id);

        /// <summary>
        /// 获取活跃用户统计数据
        /// </summary>
        Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate);

        /// <summary>
        /// 根据ID获取用户信息
        /// </summary>
        Task<UserDto> GetUserByIdAsync(string id);

        /// <summary>
        /// 获取用户增长统计数据
        /// </summary>
        Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate);

        /// <summary>
        /// 获取用户列表
        /// </summary>
        Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto);

        /// <summary>
        /// 根据ID列表批量获取用户
        /// </summary>
        Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> ids);

        /// <summary>
        /// 快速保存用户信息
        /// </summary>
        Task QuickSaveUsersAsync(QuickSaveRequestDto request);

        /// <summary>
        /// 移除用户角色
        /// </summary>
        Task<IdentityResult> RemoveRolesAsync(string id, List<string> roles);

        /// <summary>
        /// 重置用户随机密码
        /// </summary>
        Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(string id);

        /// <summary>
        /// 设置用户活动状态
        /// </summary>
        Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive);

        /// <summary>
        /// 解锁用户
        /// </summary>
        Task<IdentityResult> UnlockUserAsync(string id);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<IdentityResult> UpdateUserAsync(string id, UpdateUserDto updateUserDto);

        /// <summary>
        /// 批量导入用户
        /// </summary>
        Task<int> BatchImportUsersAsync(List<UserBatchImportItemDto> importDtos);
    }
}