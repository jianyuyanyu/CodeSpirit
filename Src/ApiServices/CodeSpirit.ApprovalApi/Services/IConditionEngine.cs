using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 条件引擎接口
/// </summary>
public interface IConditionEngine : IScopedDependency
{
    /// <summary>
    /// 评估条件表达式
    /// </summary>
    /// <param name="expression">条件表达式</param>
    /// <param name="context">上下文数据</param>
    /// <returns>评估结果</returns>
    Task<bool> EvaluateAsync(string expression, Dictionary<string, object> context);

    /// <summary>
    /// 获取下一个节点
    /// </summary>
    /// <param name="currentNodeId">当前节点ID</param>
    /// <param name="context">上下文数据</param>
    /// <returns>下一个节点列表</returns>
    Task<List<WorkflowNode>> GetNextNodesAsync(string currentNodeId, Dictionary<string, object> context);

    /// <summary>
    /// 解析审批人
    /// </summary>
    /// <param name="approverConfig">审批人配置</param>
    /// <param name="context">上下文数据</param>
    /// <returns>审批人列表</returns>
    Task<List<string>> ResolveApproversAsync(WorkflowNodeApprover approverConfig, Dictionary<string, object> context);
}
