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
    /// <param name="skipValidation">是否跳过验证（用于事件处理等场景）</param>
    /// <returns>创建的用户数据传输对象</returns>
    Task<UserDto> CreateAdvancedUserAsync(
        CreateUserDto createDto, 
        string password = null, 
        long? creatorId = null,
        long? userId = null,
        bool skipValidation = false);

    /// <summary>
    /// 根据用户名查询用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户信息</returns>
    Task<UserDto> GetUserByUserNameAsync(string userName);

    /// <summary>
    /// 根据手机号查询用户
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>用户信息</returns>
    Task<UserDto> GetUserByPhoneNumberAsync(string phoneNumber);

    /// <summary>
    /// 根据身份证号码查询用户
    /// </summary>
    /// <param name="idNo">身份证号码</param>
    /// <returns>用户信息</returns>
    Task<UserDto> GetUserByIdNoAsync(string idNo);

    /// <summary>
    /// 根据用户ID获取用户信息（忽略所有过滤器）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息</returns>
    /// <remarks>
    /// 此方法会忽略租户过滤器和软删除过滤器，主要用于获取当前登录用户自己的信息
    /// </remarks>
    Task<UserDto> GetUserByIdIgnoreFiltersAsync(long userId);

    /// <summary>
    /// 获取用户角色（忽略多租户过滤器）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>角色名称列表</returns>
    /// <remarks>
    /// 此方法会忽略租户过滤器，确保系统平台和租户平台都能正确获取到用户角色
    /// </remarks>
    Task<List<string>> GetUserRolesIgnoreFiltersAsync(long userId);

    #region 系统平台专用方法

    /// <summary>
    /// 获取系统用户列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统用户分页列表</returns>
    Task<PageList<SystemUserDto>> GetSystemUsersAsync(SystemUserQueryDto queryDto);

    /// <summary>
    /// 获取按租户分组的用户统计信息
    /// </summary>
    /// <returns>租户用户统计列表</returns>
    Task<List<TenantUserStatisticsDto>> GetUsersByTenantStatisticsAsync();

    /// <summary>
    /// 将用户转移到指定租户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetTenantId">目标租户ID</param>
    /// <returns>转移后的用户信息</returns>
    Task<UserDto> TransferUserToTenantAsync(long userId, string targetTenantId);

    /// <summary>
    /// 获取跨租户用户增长趋势对比
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>各租户用户增长趋势数据</returns>
    Task<IEnumerable<object>> GetCrossTenantUserGrowthTrendAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// 获取各租户用户活跃度排行
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>租户活跃度排行数据</returns>
    Task<IEnumerable<object>> GetTenantUserActivityRankingAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// 获取租户用户规模分布统计
    /// </summary>
    /// <returns>租户用户规模分布数据</returns>
    Task<IEnumerable<object>> GetTenantUserScaleDistributionAsync();

    /// <summary>
    /// 获取系统整体用户健康度分析
    /// </summary>
    /// <returns>用户健康度分析数据</returns>
    Task<object> GetSystemUserHealthAnalysisAsync();

    /// <summary>
    /// 获取租户用户迁移历史统计
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>用户迁移历史统计数据</returns>
    Task<IEnumerable<object>> GetUserTransferHistoryStatisticsAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    #endregion
}