using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流定义服务接口
/// </summary>
public interface IWorkflowDefinitionService : IBaseCRUDService<WorkflowDefinition, WorkflowDefinitionDto, long, CreateWorkflowDefinitionDto, UpdateWorkflowDefinitionDto>
{
    /// <summary>
    /// 根据代码获取工作流定义
    /// </summary>
    /// <param name="code">工作流代码</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>工作流定义</returns>
    Task<WorkflowDefinition?> GetByCodeAsync(string code, string? tenantId = null);

    /// <summary>
    /// 启用/禁用工作流
    /// </summary>
    /// <param name="id">工作流ID</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>操作结果</returns>
    Task<bool> SetEnabledAsync(long id, bool enabled);

    /// <summary>
    /// 复制工作流定义
    /// </summary>
    /// <param name="sourceId">源工作流ID</param>
    /// <param name="newName">新工作流名称</param>
    /// <param name="newCode">新工作流代码</param>
    /// <returns>新工作流定义</returns>
    Task<WorkflowDefinition> CopyAsync(long sourceId, string newName, string newCode);

    /// <summary>
    /// 快速保存工作流定义
    /// </summary>
    /// <param name="request">快速保存请求</param>
    /// <returns>操作结果</returns>
    Task QuickSaveWorkflowDefinitionsAsync(WorkflowDefinitionQuickSaveRequestDto request);
}
