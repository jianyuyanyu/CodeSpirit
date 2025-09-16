using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流引擎实现
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly ApprovalDbContext _context;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IConditionEngine _conditionEngine;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WorkflowEngine> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowEngine(
        ApprovalDbContext context,
        IWorkflowDefinitionService workflowDefinitionService,
        IConditionEngine conditionEngine,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ILogger<WorkflowEngine> logger)
    {
        _context = context;
        _workflowDefinitionService = workflowDefinitionService;
        _conditionEngine = conditionEngine;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// 启动工作流
    /// </summary>
    public async Task<ApprovalInstance> StartWorkflowAsync(
        string workflowCode,
        string entityType,
        string entityId,
        string applicantId,
        string title,
        object? businessData = null,
        string? tenantId = null)
    {
        try
        {
            tenantId ??= _tenantContext.TenantId;
            
            _logger.LogInformation("启动工作流: 代码={WorkflowCode}", workflowCode);

            // 获取工作流定义
            var workflowDefinition = await _workflowDefinitionService.GetByCodeAsync(workflowCode, tenantId);
            if (workflowDefinition == null)
                throw new BusinessException($"工作流定义不存在: {workflowCode}");

            if (!workflowDefinition.IsEnabled)
                throw new BusinessException($"工作流已禁用: {workflowCode}");

            // 创建审批实例
            var instance = new ApprovalInstance
            {
                TenantId = tenantId,
                WorkflowDefinitionId = workflowDefinition.Id,
                Title = title,
                EntityType = entityType,
                EntityId = entityId,
                ApplicantId = applicantId,
                ApplicantName = applicantId, // 简化处理
                Status = ApprovalStatus.InProgress,
                ApplyTime = DateTime.UtcNow,
                BusinessData = JsonConvert.SerializeObject(businessData),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(applicantId ?? "0")
            };

            _context.ApprovalInstances.Add(instance);
            await _context.SaveChangesAsync();

            _logger.LogInformation("工作流启动成功: 实例ID={InstanceId}", instance.Id);
            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动工作流失败: 代码={WorkflowCode}", workflowCode);
            throw;
        }
    }

    /// <summary>
    /// 处理审批任务
    /// </summary>
    public async Task<WorkflowProcessResult> ProcessTaskAsync(
        string taskId,
        string approverId,
        ApprovalResult result,
        string comment = "")
    {
        try
        {
            _logger.LogInformation("处理审批任务: 任务ID={TaskId}", taskId);

            var task = await _context.ApprovalTasks
                .Include(x => x.ApprovalInstance)
                .FirstOrDefaultAsync(x => x.Id == long.Parse(taskId));

            if (task == null)
                throw new BusinessException("审批任务不存在");

            if (task.ApproverId != approverId)
                throw new BusinessException("无权处理此审批任务");

            // 更新任务状态
            task.Status = ApprovalTaskStatus.Completed;
            task.Result = result;
            task.Comment = comment;
            task.ProcessedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkflowProcessResult
            {
                Success = true,
                Message = "处理成功",
                Instance = task.ApprovalInstance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批任务失败: 任务ID={TaskId}", taskId);
            throw;
        }
    }

    /// <summary>
    /// 加签
    /// </summary>
    public async Task<ApprovalTask> AddSignAsync(string taskId, string approverId, string comment = "")
    {
        var task = await _context.ApprovalTasks.FirstOrDefaultAsync(x => x.Id == long.Parse(taskId));
        if (task == null)
            throw new BusinessException("审批任务不存在");

        var newTask = new ApprovalTask
        {
            ApprovalInstanceId = task.ApprovalInstanceId,
            WorkflowNodeId = task.WorkflowNodeId,
            ApproverId = approverId,
            ApproverName = approverId,
            Status = ApprovalTaskStatus.Pending,
            AssignedTime = DateTime.UtcNow,
            IsAdditionalSign = true,
            AdditionalSignInitiatorId = task.ApproverId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Id ?? 0
        };

        _context.ApprovalTasks.Add(newTask);
        await _context.SaveChangesAsync();

        return newTask;
    }

    /// <summary>
    /// 转交任务
    /// </summary>
    public async Task<ApprovalTask> TransferTaskAsync(string taskId, string fromUserId, string toUserId, string comment = "")
    {
        var task = await _context.ApprovalTasks.FirstOrDefaultAsync(x => x.Id == long.Parse(taskId));
        if (task == null)
            throw new BusinessException("审批任务不存在");

        // 取消原任务
        task.Status = ApprovalTaskStatus.Cancelled;

        // 创建新任务
        var newTask = new ApprovalTask
        {
            ApprovalInstanceId = task.ApprovalInstanceId,
            WorkflowNodeId = task.WorkflowNodeId,
            ApproverId = toUserId,
            ApproverName = toUserId,
            Status = ApprovalTaskStatus.Pending,
            AssignedTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = long.Parse(fromUserId ?? "0")
        };

        _context.ApprovalTasks.Add(newTask);
        await _context.SaveChangesAsync();

        return newTask;
    }

    /// <summary>
    /// 撤回审批
    /// </summary>
    public async Task<bool> WithdrawAsync(string instanceId, string applicantId, string reason = "")
    {
        var instance = await _context.ApprovalInstances
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == long.Parse(instanceId));

        if (instance == null)
            return false;

        // 更新实例状态
        instance.Status = ApprovalStatus.Withdrawn;
        instance.CompletedTime = DateTime.UtcNow;

        // 取消所有待办任务
        var pendingTasks = instance.Tasks.Where(x => x.Status == ApprovalTaskStatus.Pending);
        foreach (var task in pendingTasks)
        {
            task.Status = ApprovalTaskStatus.Cancelled;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 获取用户待办任务
    /// </summary>
    public async Task<List<ApprovalTask>> GetPendingTasksAsync(string userId, string? tenantId = null)
    {
        tenantId ??= _tenantContext.TenantId;

        return await _context.ApprovalTasks
            .Include(x => x.ApprovalInstance)
            .Where(x => x.ApproverId == userId && 
                       x.Status == ApprovalTaskStatus.Pending &&
                       x.ApprovalInstance.TenantId == tenantId)
            .OrderByDescending(x => x.AssignedTime)
            .ToListAsync();
    }

    /// <summary>
    /// 获取审批实例详情
    /// </summary>
    public async Task<ApprovalInstance?> GetInstanceAsync(string instanceId)
    {
        return await _context.ApprovalInstances
            .Include(x => x.Tasks)
            .Include(x => x.WorkflowDefinition)
            .FirstOrDefaultAsync(x => x.Id == long.Parse(instanceId));
    }
}