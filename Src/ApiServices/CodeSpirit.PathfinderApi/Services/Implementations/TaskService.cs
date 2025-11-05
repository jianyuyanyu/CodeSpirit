using AutoMapper;
using CodeSpirit.Audit.LLM;
using CodeSpirit.PathfinderApi.Dtos.Task;
using CodeSpirit.PathfinderApi.Models;
using CodeSpirit.PathfinderApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.PathfinderApi.Services.Implementations;

/// <summary>
/// 任务服务实现
/// </summary>
public class TaskService : BaseCRUDService<PathfinderTask, TaskDto, Guid, CreateTaskDto, UpdateTaskDto>, ITaskService
{
    private readonly AuditableLLMAssistant _llmAssistant;
    private readonly IRepository<Models.Goal> _goalRepository;
    private readonly ILogger<TaskService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskService(
        IRepository<PathfinderTask> repository,
        IMapper mapper,
        AuditableLLMAssistant llmAssistant,
        IRepository<Models.Goal> goalRepository,
        ILogger<TaskService> logger)
        : base(repository, mapper)
    {
        _llmAssistant = llmAssistant;
        _goalRepository = goalRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取任务列表
    /// </summary>
    public async Task<PageList<TaskDto>> GetTasksAsync(TaskQueryDto queryDto)
    {
        var query = Repository.CreateQuery();
        
        // 目标ID筛选
        if (queryDto.GoalId.HasValue)
        {
            query = query.Where(t => t.GoalId == queryDto.GoalId.Value);
        }
        
        // 状态筛选
        if (queryDto.Status.HasValue)
        {
            query = query.Where(t => t.Status == queryDto.Status.Value);
        }
        
        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            query = query.Where(t => 
                t.Title.Contains(queryDto.Keywords) || 
                (t.Description != null && t.Description.Contains(queryDto.Keywords)));
        }
        
        // 优先级范围筛选
        if (queryDto.MinPriority.HasValue)
        {
            query = query.Where(t => t.Priority >= queryDto.MinPriority.Value);
        }
        
        if (queryDto.MaxPriority.HasValue)
        {
            query = query.Where(t => t.Priority <= queryDto.MaxPriority.Value);
        }
        
        // 日期范围筛选
        if (queryDto.StartDate.HasValue)
        {
            query = query.Where(t => t.EstimatedStartTime >= queryDto.StartDate.Value);
        }
        
        if (queryDto.EndDate.HasValue)
        {
            query = query.Where(t => t.EstimatedEndTime <= queryDto.EndDate.Value);
        }
        
        // 排序
        query = query.OrderByDescending(t => t.Priority).ThenBy(t => t.EstimatedStartTime);
        
        // 分页
        var total = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();
        
        var dtos = Mapper.Map<List<TaskDto>>(items);
        
        return new PageList<TaskDto>(dtos, total);
    }
    
    /// <summary>
    /// 更新任务状态
    /// </summary>
    public async Task<bool> UpdateTaskStatusAsync(Guid taskId, Models.Enums.TaskStatus status)
    {
        var task = await Repository.GetByIdAsync(taskId);
        if (task == null)
        {
            return false;
        }
        
        task.Status = status;
        
        // 更新实际时间
        if (status == Models.Enums.TaskStatus.InProgress && task.ActualStartTime == null)
        {
            task.ActualStartTime = DateTime.UtcNow;
        }
        else if (status == Models.Enums.TaskStatus.Completed && task.ActualEndTime == null)
        {
            task.ActualEndTime = DateTime.UtcNow;
        }
        
        await Repository.UpdateAsync(task);
        await Repository.SaveChangesAsync();
        
        _logger.LogInformation("任务状态已更新: {TaskId} -> {Status}", taskId, status);
        
        return true;
    }
    
    /// <summary>
    /// 获取任务依赖链
    /// </summary>
    public async Task<List<TaskDto>> GetTaskDependenciesAsync(Guid taskId)
    {
        var task = await Repository.GetByIdAsync(taskId);
        if (task == null || string.IsNullOrWhiteSpace(task.DependsOn))
        {
            return new List<TaskDto>();
        }
        
        var dependencyIds = task.DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Guid.TryParse(x.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToList();
        
        if (!dependencyIds.Any())
        {
            return new List<TaskDto>();
        }
        
        var dependencyTasks = await Repository.CreateQuery()
            .Where(t => dependencyIds.Contains(t.Id))
            .ToListAsync();
        
        return Mapper.Map<List<TaskDto>>(dependencyTasks);
    }
    
    /// <summary>
    /// 批量创建任务
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建的任务列表</returns>
    public async Task<List<TaskDto>> BatchCreateTasksAsync(BatchCreateTasksRequest request)
    {
        try
        {
            // 1. 验证目标是否存在
            var goal = await _goalRepository.GetByIdAsync(request.GoalId);
            if (goal == null)
            {
                throw new BusinessException("目标不存在");
            }
            
            // 2. 创建任务实体列表
            var taskEntities = new List<PathfinderTask>();
            var taskIdMapping = new Dictionary<int, Guid>(); // 序号 -> 实际ID 的映射
            
            for (int i = 0; i < request.Tasks.Count; i++)
            {
                var createDto = request.Tasks[i];
                var taskEntity = new PathfinderTask
                {
                    Id = Guid.NewGuid(),
                    GoalId = request.GoalId,
                    Title = createDto.Title,
                    Description = createDto.Description,
                    Priority = createDto.Priority,
                    EstimatedStartTime = createDto.EstimatedStartTime,
                    EstimatedEndTime = createDto.EstimatedEndTime,
                    Status = Models.Enums.TaskStatus.Pending,
                    DependsOn = null // 第一轮先创建，依赖关系后面更新
                };
                
                await Repository.AddAsync(taskEntity);
                taskEntities.Add(taskEntity);
                taskIdMapping[i + 1] = taskEntity.Id; // 序号从1开始
            }
            
            // 3. 保存到数据库
            await Repository.SaveChangesAsync();
            
            // 4. 第二轮：更新依赖关系（将序号转换为实际的Guid）
            for (int i = 0; i < request.Tasks.Count; i++)
            {
                var createDto = request.Tasks[i];
                if (!string.IsNullOrWhiteSpace(createDto.DependsOn))
                {
                    var dependencyIndices = createDto.DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x.Trim(), out var index) ? index : 0)
                        .Where(x => x > 0)
                        .ToList();
                    
                    var dependencyGuids = dependencyIndices
                        .Where(idx => taskIdMapping.ContainsKey(idx))
                        .Select(idx => taskIdMapping[idx])
                        .ToList();
                    
                    if (dependencyGuids.Any())
                    {
                        taskEntities[i].DependsOn = string.Join(",", dependencyGuids);
                    }
                }
            }
            
            // 5. 保存依赖关系
            await Repository.SaveChangesAsync();
            
            _logger.LogInformation("批量创建任务成功，共创建 {Count} 个任务，目标ID: {GoalId}", 
                taskEntities.Count, request.GoalId);
            
            return Mapper.Map<List<TaskDto>>(taskEntities);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建任务失败: {GoalId}", request.GoalId);
            throw new Exception($"批量创建任务失败：{ex.Message}");
        }
    }
}

