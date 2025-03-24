using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService : IBaseCRUDIService<ApplicationUser, UserDto, long, CreateUserDto, UpdateUserDto, UserBatchImportItemDto>, IScopedDependency
{
    /// <summary>
    /// 获取用户列表（分页）
    /// </summary>
    Task<PageList<UserDto>> GetUsersAsync(UserQueryDto queryDto);

    /// <summary>
    /// 分配角色
    /// </summary>
    Task AssignRolesAsync(long id, List<string> roles);

    /// <summary>
    /// 移除角色
    /// </summary>
    Task RemoveRolesAsync(long id, List<string> roles);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task UpdateUserAsync(long id, UpdateUserDto updateUserDto);

    /// <summary>
    /// 设置用户激活状态
    /// </summary>
    Task SetActiveStatusAsync(long id, bool isActive);

    /// <summary>
    /// 重置随机密码
    /// </summary>
    Task<string> ResetRandomPasswordAsync(long id);

    /// <summary>
    /// 解锁用户
    /// </summary>
    Task UnlockUserAsync(long id);

    /// <summary>
    /// 根据ID列表获取用户
    /// </summary>
    Task<List<ApplicationUser>> GetUsersByIdsAsync(List<long> ids);

    /// <summary>
    /// 快速保存用户信息
    /// </summary>
    Task QuickSaveUsersAsync(QuickSaveRequestDto request);

    /// <summary>
    /// 获取用户增长趋势
    /// </summary>
    Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// 获取活跃用户统计
    /// </summary>
    Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    Task<IEnumerable<object>> GetInactiveUsersStatisticsAsync(int thresholdDays);
    Task<IEnumerable<object>> GetUserLoginFrequencyAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    Task<IEnumerable<object>> GetUserRegistrationTrendAsync(DateTimeOffset startDate, DateTimeOffset endDate, string groupBy);
    Task<IEnumerable<object>> GetUserActiveStatusDistributionAsync();
    Task<IEnumerable<object>> GetUserGenderDistributionAsync();

    /// <summary>
    /// 高级用户创建方法，支持指定密码及创建者等信息
    /// </summary>
    /// <param name="createDto">用户创建数据传输对象</param>
    /// <param name="password">指定的用户密码，如为null则自动生成随机密码</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="userId"> userId不为空时,将userId作为新创建的用户的Id,否则将自动生成Id</param>
    /// <param name="creatorName">创建者名称</param>
    /// <returns>创建的用户数据传输对象</returns>
    Task<UserDto> CreateAdvancedUserAsync(
        CreateUserDto createDto, 
        string password = null, 
        long? creatorId = null,
        long? userId = null);
}