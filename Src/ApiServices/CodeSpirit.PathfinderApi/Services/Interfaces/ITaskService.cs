using CodeSpirit.PathfinderApi.Dtos.Task;
using CodeSpirit.PathfinderApi.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.PathfinderApi.Services.Interfaces;

/// <summary>
/// 任务服务接口
/// </summary>
public interface ITaskService : IBaseCRUDService<PathfinderTask, TaskDto, Guid, CreateTaskDto, UpdateTaskDto>
{
    /// <summary>
    /// 获取任务列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>任务列表</returns>
    Task<PageList<TaskDto>> GetTasksAsync(TaskQueryDto queryDto);
    
    /// <summary>
    /// 更新任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="status">新状态</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateTaskStatusAsync(Guid taskId, Models.Enums.TaskStatus status);
    
    /// <summary>
    /// 获取任务依赖链
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>依赖任务列表</returns>
    Task<List<TaskDto>> GetTaskDependenciesAsync(Guid taskId);
    
    /// <summary>
    /// 批量创建任务
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建的任务列表</returns>
    Task<List<TaskDto>> BatchCreateTasksAsync(BatchCreateTasksRequest request);
}

