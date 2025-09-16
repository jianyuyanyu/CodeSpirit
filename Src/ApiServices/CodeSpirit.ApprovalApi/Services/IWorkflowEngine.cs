using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流引擎接口
/// </summary>
public interface IWorkflowEngine : IScopedDependency
{
    /// <summary>
    /// 启动工作流
    /// </summary>
    /// <param name="workflowCode">工作流代码</param>
    /// <param name="entityType">业务实体类型</param>
    /// <param name="entityId">业务实体ID</param>
    /// <param name="applicantId">申请人ID</param>
    /// <param name="title">审批标题</param>
    /// <param name="businessData">业务数据</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>审批实例</returns>
    Task<ApprovalInstance> StartWorkflowAsync(
        string workflowCode,
        string entityType,
        string entityId,
        string applicantId,
        string title,
        object? businessData = null,
        string? tenantId = null);

    /// <summary>
    /// 处理审批任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="approverId">审批人ID</param>
    /// <param name="result">审批结果</param>
    /// <param name="comment">审批意见</param>
    /// <returns>处理结果</returns>
    Task<WorkflowProcessResult> ProcessTaskAsync(
        string taskId,
        string approverId,
        ApprovalResult result,
        string comment = "");

    /// <summary>
    /// 加签
    /// </summary>
    /// <param name="taskId">当前任务ID</param>
    /// <param name="approverId">加签人ID</param>
    /// <param name="comment">加签理由</param>
    /// <returns>加签结果</returns>
    Task<ApprovalTask> AddSignAsync(
        string taskId,
        string approverId,
        string comment = "");

    /// <summary>
    /// 转交任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="fromUserId">转交人ID</param>
    /// <param name="toUserId">接收人ID</param>
    /// <param name="comment">转交理由</param>
    /// <returns>转交结果</returns>
    Task<ApprovalTask> TransferTaskAsync(
        string taskId,
        string fromUserId,
        string toUserId,
        string comment = "");

    /// <summary>
    /// 撤回审批
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <param name="applicantId">申请人ID</param>
    /// <param name="reason">撤回理由</param>
    /// <returns>撤回结果</returns>
    Task<bool> WithdrawAsync(
        string instanceId,
        string applicantId,
        string reason = "");

    /// <summary>
    /// 获取用户待办任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>待办任务列表</returns>
    Task<List<ApprovalTask>> GetPendingTasksAsync(
        string userId,
        string? tenantId = null);

    /// <summary>
    /// 获取审批实例详情
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>审批实例</returns>
    Task<ApprovalInstance?> GetInstanceAsync(string instanceId);
}

/// <summary>
/// 工作流处理结果
/// </summary>
public class WorkflowProcessResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 审批实例
    /// </summary>
    public ApprovalInstance? Instance { get; set; }

    /// <summary>
    /// 下一步任务
    /// </summary>
    public List<ApprovalTask> NextTasks { get; set; } = new();
}
