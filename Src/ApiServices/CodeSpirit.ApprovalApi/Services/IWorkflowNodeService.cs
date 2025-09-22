using CodeSpirit.ApprovalApi.Dtos.WorkflowNode;
using CodeSpirit.ApprovalApi.Dtos.Visualization;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流节点服务接口
/// </summary>
public interface IWorkflowNodeService : IBaseCRUDService<WorkflowNode, WorkflowNodeDto, long, CreateWorkflowNodeDto, UpdateWorkflowNodeDto>
{
    /// <summary>
    /// 根据工作流定义ID获取节点列表
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>节点列表</returns>
    Task<List<WorkflowNodeDto>> GetByWorkflowDefinitionIdAsync(long workflowDefinitionId);

    /// <summary>
    /// 批量创建工作流节点
    /// </summary>
    /// <param name="dto">批量创建DTO</param>
    /// <returns>创建的节点列表</returns>
    Task<List<WorkflowNodeDto>> BatchCreateAsync(BatchCreateWorkflowNodesDto dto);

    /// <summary>
    /// 保存流程设计
    /// </summary>
    /// <param name="dto">流程设计DTO</param>
    /// <returns>操作结果</returns>
    Task<bool> SaveProcessDesignAsync(WorkflowProcessDesignDto dto);

    /// <summary>
    /// 验证工作流节点配置
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="nodes">节点列表</param>
    /// <returns>验证结果</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateNodesAsync(long workflowDefinitionId, List<CreateWorkflowNodeDto> nodes);

    /// <summary>
    /// 复制工作流节点
    /// </summary>
    /// <param name="sourceWorkflowDefinitionId">源工作流定义ID</param>
    /// <param name="targetWorkflowDefinitionId">目标工作流定义ID</param>
    /// <returns>复制的节点列表</returns>
    Task<List<WorkflowNodeDto>> CopyNodesAsync(long sourceWorkflowDefinitionId, long targetWorkflowDefinitionId);

    /// <summary>
    /// 删除工作流定义的所有节点
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>操作结果</returns>
    Task<bool> DeleteByWorkflowDefinitionIdAsync(long workflowDefinitionId);

    /// <summary>
    /// 获取工作流预览数据
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>预览数据</returns>
    Task<WorkflowPreviewDto> GetWorkflowPreviewAsync(long workflowDefinitionId);

    /// <summary>
    /// 导入工作流节点
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="items">导入项列表</param>
    /// <returns>导入结果</returns>
    Task<(int SuccessCount, int ErrorCount, List<string> Errors)> ImportNodesAsync(
        long workflowDefinitionId, 
        List<WorkflowNodeBatchImportItemDto> items);

    /// <summary>
    /// 获取工作流可视化数据
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>可视化数据</returns>
    Task<WorkflowVisualizationDto> GetWorkflowVisualizationAsync(long workflowDefinitionId);

    /// <summary>
    /// 高级验证工作流
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="nodes">节点列表</param>
    /// <returns>验证结果</returns>
    Task<WorkflowValidationResultDto> AdvancedValidateWorkflowAsync(long workflowDefinitionId, List<CreateWorkflowNodeDto> nodes);

    /// <summary>
    /// 获取工作流实例状态
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>实例状态</returns>
    Task<WorkflowInstanceStatusDto> GetWorkflowInstanceStatusAsync(long instanceId);
}
