using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 条件引擎实现
/// </summary>
public class ConditionEngine : IConditionEngine
{
    private readonly ApprovalDbContext _context;
    private readonly ILogger<ConditionEngine> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="serviceProvider">服务提供者</param>
    public ConditionEngine(
        ApprovalDbContext context,
        ILogger<ConditionEngine> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 评估条件表达式
    /// </summary>
    /// <param name="expression">条件表达式</param>
    /// <param name="context">上下文数据</param>
    /// <returns>评估结果</returns>
    public async Task<bool> EvaluateAsync(string expression, Dictionary<string, object> context)
    {
        try
        {
            if (string.IsNullOrEmpty(expression))
                return true;

            _logger.LogDebug("评估条件表达式: {Expression}", expression);

            // 替换表达式中的变量
            var evaluatedExpression = await ReplaceVariablesAsync(expression, context);

            // 简单的表达式评估器
            var result = EvaluateSimpleExpression(evaluatedExpression);

            _logger.LogDebug("条件表达式评估结果: {Expression} => {Result}", expression, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "评估条件表达式失败: {Expression}", expression);
            return false;
        }
    }

    /// <summary>
    /// 获取下一个节点
    /// </summary>
    /// <param name="currentNodeId">当前节点ID</param>
    /// <param name="context">上下文数据</param>
    /// <returns>下一个节点列表</returns>
    public async Task<List<WorkflowNode>> GetNextNodesAsync(string currentNodeId, Dictionary<string, object> context)
    {
        try
        {
            var currentNode = await _context.WorkflowNodes
                .Include(x => x.Conditions)
                .Include(x => x.WorkflowDefinition)
                .ThenInclude(x => x.Nodes)
                .FirstOrDefaultAsync(x => x.Id == long.Parse(currentNodeId));

            if (currentNode == null)
                return new List<WorkflowNode>();

            var nextNodes = new List<WorkflowNode>();

            // 如果是开始节点，查找第一个审批节点
            if (currentNode.NodeType == WorkflowNodeType.Start)
            {
                var firstApprovalNode = currentNode.WorkflowDefinition.Nodes
                    .FirstOrDefault(x => x.NodeType == WorkflowNodeType.Approval);
                
                if (firstApprovalNode != null)
                {
                    nextNodes.Add(firstApprovalNode);
                }
            }
            // 如果有条件配置，根据条件选择下一个节点
            else if (currentNode.Conditions.Any())
            {
                foreach (var condition in currentNode.Conditions.OrderBy(x => x.Id))
                {
                    var conditionResult = await EvaluateAsync(condition.Expression, context);
                    if (conditionResult)
                    {
                        var nextNode = currentNode.WorkflowDefinition.Nodes
                            .FirstOrDefault(x => x.Name == condition.NextNodeName);
                        
                        if (nextNode != null)
                        {
                            nextNodes.Add(nextNode);
                        }
                        break; // 找到第一个满足条件的节点就退出
                    }
                }
            }
            // 如果没有条件配置，查找默认的下一个节点（按顺序）
            else
            {
                var allNodes = currentNode.WorkflowDefinition.Nodes.OrderBy(x => x.Id).ToList();
                var currentIndex = allNodes.FindIndex(x => x.Id == currentNode.Id);
                
                if (currentIndex >= 0 && currentIndex < allNodes.Count - 1)
                {
                    nextNodes.Add(allNodes[currentIndex + 1]);
                }
            }

            _logger.LogDebug("获取下一个节点: 当前节点={CurrentNodeId}, 下一个节点数={NextNodeCount}", 
                currentNodeId, nextNodes.Count);

            return nextNodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取下一个节点失败: 当前节点={CurrentNodeId}", currentNodeId);
            return new List<WorkflowNode>();
        }
    }

    /// <summary>
    /// 解析审批人
    /// </summary>
    /// <param name="approverConfig">审批人配置</param>
    /// <param name="context">上下文数据</param>
    /// <returns>审批人列表</returns>
    public async Task<List<string>> ResolveApproversAsync(WorkflowNodeApprover approverConfig, Dictionary<string, object> context)
    {
        try
        {
            var approvers = new List<string>();

            switch (approverConfig.ApproverType)
            {
                case ApproverType.User:
                    // 指定用户
                    approvers.Add(approverConfig.ApproverValue);
                    break;

                case ApproverType.Role:
                    // 根据角色获取用户列表
                    var roleUsers = await GetUsersByRoleAsync(approverConfig.ApproverValue);
                    approvers.AddRange(roleUsers);
                    break;

                case ApproverType.Department:
                    // 根据部门获取用户列表
                    var deptUsers = await GetUsersByDepartmentAsync(approverConfig.ApproverValue);
                    approvers.AddRange(deptUsers);
                    break;

                case ApproverType.Initiator:
                    // 发起人
                    if (context.TryGetValue("applicantId", out var applicantId))
                    {
                        approvers.Add(applicantId.ToString()!);
                    }
                    break;

                case ApproverType.InitiatorSuperior:
                    // 发起人上级
                    if (context.TryGetValue("applicantId", out var applicant))
                    {
                        var superior = await GetUserSuperiorAsync(applicant.ToString()!, 
                            int.Parse(approverConfig.ApproverValue));
                        if (!string.IsNullOrEmpty(superior))
                        {
                            approvers.Add(superior);
                        }
                    }
                    break;

                case ApproverType.Expression:
                    // 动态表达式
                    var expressionResult = await EvaluateApproverExpressionAsync(approverConfig.ApproverValue, context);
                    approvers.AddRange(expressionResult);
                    break;
            }

            _logger.LogDebug("解析审批人: 类型={ApproverType}, 配置={ApproverValue}, 结果数={Count}", 
                approverConfig.ApproverType, approverConfig.ApproverValue, approvers.Count);

            return approvers.Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析审批人失败: 类型={ApproverType}, 配置={ApproverValue}", 
                approverConfig.ApproverType, approverConfig.ApproverValue);
            return new List<string>();
        }
    }

    /// <summary>
    /// 替换表达式中的变量
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="context">上下文</param>
    /// <returns>替换后的表达式</returns>
    private async Task<string> ReplaceVariablesAsync(string expression, Dictionary<string, object> context)
    {
        var result = expression;

        // 替换 businessData.xxx 格式的变量
        var businessDataPattern = @"businessData\.(\w+)";
        var matches = Regex.Matches(expression, businessDataPattern);

        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value;
            var value = GetBusinessDataProperty(context, propertyName);
            result = result.Replace(match.Value, value?.ToString() ?? "null");
        }

        // 替换其他上下文变量
        foreach (var kvp in context)
        {
            if (kvp.Key != "businessData")
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "null");
            }
        }

        return result;
    }

    /// <summary>
    /// 获取业务数据属性值
    /// </summary>
    /// <param name="context">上下文</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>属性值</returns>
    private object? GetBusinessDataProperty(Dictionary<string, object> context, string propertyName)
    {
        if (!context.TryGetValue("businessData", out var businessData))
            return null;

        if (businessData is JObject jObject)
        {
            return jObject[propertyName]?.Value<object>();
        }

        // 使用反射获取属性值
        var property = businessData.GetType().GetProperty(propertyName);
        return property?.GetValue(businessData);
    }

    /// <summary>
    /// 简单表达式评估器
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>评估结果</returns>
    private bool EvaluateSimpleExpression(string expression)
    {
        try
        {
            // 处理常见的比较操作
            if (expression.Contains(">="))
            {
                var parts = expression.Split(">=");
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left >= right;
                }
            }
            else if (expression.Contains("<="))
            {
                var parts = expression.Split("<=");
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left <= right;
                }
            }
            else if (expression.Contains(">"))
            {
                var parts = expression.Split(">");
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left > right;
                }
            }
            else if (expression.Contains("<"))
            {
                var parts = expression.Split("<");
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left < right;
                }
            }
            else if (expression.Contains("=="))
            {
                var parts = expression.Split("==");
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim().Trim('"');
                    var right = parts[1].Trim().Trim('"');
                    return left == right;
                }
            }
            else if (expression.Contains("!="))
            {
                var parts = expression.Split("!=");
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim().Trim('"');
                    var right = parts[1].Trim().Trim('"');
                    return left != right;
                }
            }

            // 处理布尔值
            if (bool.TryParse(expression.Trim(), out var boolResult))
            {
                return boolResult;
            }

            // 默认返回true
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 根据角色获取用户列表
    /// </summary>
    /// <param name="roleCode">角色代码</param>
    /// <returns>用户ID列表</returns>
    private async Task<List<string>> GetUsersByRoleAsync(string roleCode)
    {
        // 这里应该调用身份认证服务获取角色下的用户
        // 暂时返回空列表
        await Task.CompletedTask;
        return new List<string>();
    }

    /// <summary>
    /// 根据部门获取用户列表
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <returns>用户ID列表</returns>
    private async Task<List<string>> GetUsersByDepartmentAsync(string departmentId)
    {
        // 这里应该调用组织架构服务获取部门下的用户
        // 暂时返回空列表
        await Task.CompletedTask;
        return new List<string>();
    }

    /// <summary>
    /// 获取用户上级
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="level">上级层级</param>
    /// <returns>上级用户ID</returns>
    private async Task<string?> GetUserSuperiorAsync(string userId, int level = 1)
    {
        // 这里应该调用组织架构服务获取用户上级
        // 暂时返回null
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// 评估审批人表达式
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="context">上下文</param>
    /// <returns>审批人列表</returns>
    private async Task<List<string>> EvaluateApproverExpressionAsync(string expression, Dictionary<string, object> context)
    {
        // 这里可以实现复杂的审批人表达式解析
        // 例如：getUsersByCondition(department='IT' AND level>=3)
        // 暂时返回空列表
        await Task.CompletedTask;
        return new List<string>();
    }
}
