using CodeSpirit.PathfinderApi.Dtos.Goal;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.PathfinderApi.Services.Interfaces;

/// <summary>
/// 目标服务接口
/// </summary>
public interface IGoalService : IBaseCRUDService<Models.Goal, GoalDto, Guid, CreateGoalDto, UpdateGoalDto>
{
    /// <summary>
    /// 创建目标（包含AI解析）
    /// </summary>
    /// <param name="dto">创建目标DTO</param>
    /// <returns>创建的目标</returns>
    Task<GoalDto> CreateGoalAsync(CreateGoalDto dto);
    
    /// <summary>
    /// 获取目标列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>目标列表</returns>
    Task<PageList<GoalDto>> GetGoalsAsync(GoalQueryDto queryDto);
    
    /// <summary>
    /// 更新目标
    /// </summary>
    /// <param name="id">目标ID</param>
    /// <param name="dto">更新DTO</param>
    /// <returns>更新后的目标</returns>
    Task<GoalDto> UpdateGoalAsync(Guid id, UpdateGoalDto dto);
}

