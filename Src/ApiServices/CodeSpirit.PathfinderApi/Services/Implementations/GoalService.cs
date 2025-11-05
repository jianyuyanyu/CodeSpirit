using AutoMapper;
using CodeSpirit.Audit.LLM;
using CodeSpirit.Core;
using CodeSpirit.PathfinderApi.Dtos.Goal;
using CodeSpirit.PathfinderApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.PathfinderApi.Services.Implementations;

/// <summary>
/// 目标服务实现
/// </summary>
public class GoalService : BaseCRUDService<Models.Goal, GoalDto, Guid, CreateGoalDto, UpdateGoalDto>, IGoalService
{
    private readonly AuditableLLMAssistant _llmAssistant;
    private readonly ILogger<GoalService> _logger;
    private readonly ICurrentUser _currentUser;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="llmAssistant">可审计的LLM助手</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="logger">日志记录器</param>
    public GoalService(
        IRepository<Models.Goal> repository,
        IMapper mapper,
        AuditableLLMAssistant llmAssistant,
        ICurrentUser currentUser,
        ILogger<GoalService> logger)
        : base(repository, mapper)
    {
        _llmAssistant = llmAssistant;
        _currentUser = currentUser;
        _logger = logger;
    }
    
    /// <summary>
    /// 创建目标（包含AI评估结果）
    /// </summary>
    /// <param name="dto">创建目标DTO</param>
    /// <returns>创建的目标</returns>
    public async Task<GoalDto> CreateGoalAsync(CreateGoalDto dto)
    {
        var goal = new Models.Goal
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.Id ?? throw new UnauthorizedAccessException("用户未登录"),
            Title = !string.IsNullOrWhiteSpace(dto.Title) 
                ? dto.Title 
                : (dto.Description.Length > 20 ? dto.Description.Substring(0, 20) + "..." : dto.Description),
            Description = dto.Description,
            Category = dto.Category ?? "其他",
            TargetDate = dto.TargetDate,
            Status = Models.Enums.GoalStatus.Active,
            Progress = 0,
            // 保存前端AI评估结果
            ClarityScore = dto.ClarityScore,
            ExecutabilityScore = dto.ExecutabilityScore,
            CompletenessScore = dto.CompletenessScore,
            // 计算综合评分（如果三维评分都存在）
            FeasibilityScore = dto.ClarityScore.HasValue && dto.ExecutabilityScore.HasValue && dto.CompletenessScore.HasValue
                ? (int)Math.Round((dto.ClarityScore.Value + dto.ExecutabilityScore.Value + dto.CompletenessScore.Value) / 3.0)
                : null
        };
        
        await Repository.AddAsync(goal);
        await Repository.SaveChangesAsync();
        
        _logger.LogInformation("创建目标成功: {GoalId}, 用户: {UserId}, 综合评分: {FeasibilityScore}", 
            goal.Id, _currentUser.Id, goal.FeasibilityScore);
        
        return Mapper.Map<GoalDto>(goal);
    }
    
    /// <summary>
    /// 获取目标列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>目标列表</returns>
    public async Task<PageList<GoalDto>> GetGoalsAsync(GoalQueryDto queryDto)
    {
        var query = Repository.CreateQuery();
        
        // 用户ID筛选
        if (queryDto.UserId.HasValue)
        {
            query = query.Where(g => g.UserId == queryDto.UserId.Value);
        }
        // 注意：当前版本暂时不过滤用户，后续需要结合身份服务
        
        // 状态筛选
        if (queryDto.Status.HasValue)
        {
            query = query.Where(g => g.Status == queryDto.Status.Value);
        }
        
        // 类型筛选
        if (!string.IsNullOrWhiteSpace(queryDto.Category))
        {
            query = query.Where(g => g.Category == queryDto.Category);
        }
        
        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keyword))
        {
            query = query.Where(g => 
                g.Title.Contains(queryDto.Keyword) || 
                (g.Description != null && g.Description.Contains(queryDto.Keyword)));
        }
        
        // 日期范围筛选
        if (queryDto.StartDate.HasValue)
        {
            query = query.Where(g => g.CreatedAt >= queryDto.StartDate.Value);
        }
        
        if (queryDto.EndDate.HasValue)
        {
            query = query.Where(g => g.CreatedAt <= queryDto.EndDate.Value);
        }
        
        // 排序
        query = query.OrderByDescending(g => g.CreatedAt);
        
        // 分页
        var total = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();
        
        var dtos = Mapper.Map<List<GoalDto>>(items);
        
        return new PageList<GoalDto>(dtos, total);
    }
    
    /// <summary>
    /// 更新目标
    /// </summary>
    /// <param name="id">目标ID</param>
    /// <param name="dto">更新DTO</param>
    /// <returns>更新后的目标</returns>
    public async Task<GoalDto> UpdateGoalAsync(Guid id, UpdateGoalDto dto)
    {
        var goal = await Repository.GetByIdAsync(id);
        if (goal == null)
        {
            throw new BusinessException($"目标不存在: {id}");
        }
        
        // 检查权限 (当前版本暂时跳过权限检查，后续需要结合身份服务)
        // if (goal.UserId != currentUserGuid)
        // {
        //     throw new BusinessException("无权限更新此目标");
        // }
        
        // 更新字段
        Mapper.Map(dto, goal);
        
        await Repository.UpdateAsync(goal);
        await Repository.SaveChangesAsync();
        
        _logger.LogInformation("更新目标成功: {GoalId}", id);
        
        return Mapper.Map<GoalDto>(goal);
    }
}
