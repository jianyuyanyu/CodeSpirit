using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos.ApprovalTask;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.EventBus.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批任务服务实现
/// </summary>
public class ApprovalTaskService : BaseCRUDService<ApprovalTask, ApprovalTaskDto, long, ProcessApprovalTaskDto, ProcessApprovalTaskDto>, IApprovalTaskService
{
    private readonly ApprovalDbContext _context;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IApprovalLogService _approvalLogService;
    private readonly IIntelligentApprovalService _intelligentApprovalService;
    private readonly ITenantAwareEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IClientIpService _clientIpService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApprovalTaskService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="workflowEngine">工作流引擎</param>
    /// <param name="approvalLogService">审批日志服务</param>
    /// <param name="intelligentApprovalService">智能审批服务</param>
    /// <param name="eventBus">事件总线</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <param name="clientIpService">客户端IP服务</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <param name="logger">日志记录器</param>
    public ApprovalTaskService(
        IRepository<ApprovalTask> repository,
        IMapper mapper,
        ApprovalDbContext context,
        IWorkflowEngine workflowEngine,
        IApprovalLogService approvalLogService,
        IIntelligentApprovalService intelligentApprovalService,
        ITenantAwareEventBus eventBus,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IClientIpService clientIpService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApprovalTaskService> logger)
        : base(repository, mapper)
    {
        _context = context;
        _workflowEngine = workflowEngine;
        _approvalLogService = approvalLogService;
        _intelligentApprovalService = intelligentApprovalService;
        _eventBus = eventBus;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _clientIpService = clientIpService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 处理审批任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="dto">处理审批任务DTO</param>
    /// <returns>处理结果</returns>
    public async Task<bool> ProcessTaskAsync(long taskId, ProcessApprovalTaskDto dto)
    {
        try
        {
            _logger.LogInformation("开始处理审批任务: 任务ID={TaskId}, 结果={Result}", taskId, dto.Result);

            var task = await _context.ApprovalTasks
                .Include(x => x.ApprovalInstance)
                .FirstOrDefaultAsync(x => x.Id == taskId);

            if (task == null)
                throw new BusinessException("审批任务不存在");

            if (task.ApproverId != _currentUser.Id?.ToString())
                throw new BusinessException("无权处理此审批任务");

            if (task.Status != ApprovalTaskStatus.Pending)
                throw new BusinessException("任务状态不允许处理");

            // 调用工作流引擎处理任务
            var result = await _workflowEngine.ProcessTaskAsync(
                taskId.ToString(),
                _currentUser.Id?.ToString() ?? "system",
                dto.Result,
                dto.Comment);

            if (result.Success)
            {
                // 记录审批日志
                var logType = dto.Result switch
                {
                    ApprovalResult.Approve => ApprovalLogType.Approve,
                    ApprovalResult.Reject => ApprovalLogType.Reject,
                    ApprovalResult.Transfer => ApprovalLogType.Transfer,
                    ApprovalResult.AdditionalSign => ApprovalLogType.AdditionalSign,
                    _ => ApprovalLogType.Approve
                };

                await _approvalLogService.LogAsync(new ApprovalLog
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = _tenantContext.TenantId ?? string.Empty,
                    ApprovalInstanceId = task.ApprovalInstanceId,
                    TaskId = taskId,
                    LogType = logType,
                    OperatorId = _currentUser.Id?.ToString() ?? "system",
                    OperatorName = _currentUser.UserName ?? "system",
                    OperationTime = DateTime.UtcNow,
                    Result = dto.Result,
                    Content = dto.Comment,
                    IpAddress = _clientIpService.GetClientIp(_httpContextAccessor),
                    UserAgent = _httpContextAccessor.GetUserAgent()
                });

                // 发布任务处理完成事件
                await _eventBus.PublishAsync(new TaskCompletedEvent
                {
                    TenantId = _tenantContext.TenantId ?? string.Empty,
                    InstanceId = task.ApprovalInstanceId.ToString(),
                    TaskId = taskId.ToString(),
                    ApproverId = _currentUser.Id?.ToString() ?? "system",
                    Result = dto.Result,
                    Comment = dto.Comment,
                    ProcessedTime = DateTime.UtcNow,
                    EntityType = task.ApprovalInstance.EntityType,
                    EntityId = task.ApprovalInstance.EntityId
                });

                _logger.LogInformation("审批任务处理成功: 任务ID={TaskId}, 结果={Result}", taskId, dto.Result);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批任务失败: 任务ID={TaskId}", taskId);
            throw;
        }
    }

    /// <summary>
    /// 获取我的待办任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>待办任务分页列表</returns>
    public async Task<PageList<ApprovalTaskDto>> GetMyPendingTasksAsync(string userId, int pageIndex = 1, int pageSize = 20)
    {
        var query = _context.ApprovalTasks
            .Include(x => x.ApprovalInstance)
            .ThenInclude(x => x.WorkflowDefinition)
            .Where(x => x.ApproverId == userId && x.Status == ApprovalTaskStatus.Pending)
            .OrderByDescending(x => x.AssignedTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = Mapper.Map<List<ApprovalTaskDto>>(items);

        return new PageList<ApprovalTaskDto>
        {
            Items = dtos,
            Total = totalCount
        };
    }

    /// <summary>
    /// 获取我的已办任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>已办任务分页列表</returns>
    public async Task<PageList<ApprovalTaskDto>> GetMyCompletedTasksAsync(string userId, int pageIndex = 1, int pageSize = 20)
    {
        var query = _context.ApprovalTasks
            .Include(x => x.ApprovalInstance)
            .ThenInclude(x => x.WorkflowDefinition)
            .Where(x => x.ApproverId == userId && x.Status == ApprovalTaskStatus.Completed)
            .OrderByDescending(x => x.ProcessedTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = Mapper.Map<List<ApprovalTaskDto>>(items);

        return new PageList<ApprovalTaskDto>
        {
            Items = dtos,
            Total = totalCount
        };
    }

    /// <summary>
    /// 加签
    /// </summary>
    /// <param name="taskId">当前任务ID</param>
    /// <param name="approverId">加签人ID</param>
    /// <param name="comment">加签理由</param>
    /// <returns>加签结果</returns>
    public async Task<ApprovalTask> AddSignAsync(long taskId, string approverId, string comment = "")
    {
        try
        {
            _logger.LogInformation("开始加签: 任务ID={TaskId}, 加签人={ApproverId}", taskId, approverId);

            var task = await _context.ApprovalTasks
                .Include(x => x.ApprovalInstance)
                .FirstOrDefaultAsync(x => x.Id == taskId);

            if (task == null)
                throw new BusinessException("审批任务不存在");

            if (task.ApproverId != _currentUser.Id?.ToString())
                throw new BusinessException("无权对此任务进行加签");

            if (task.Status != ApprovalTaskStatus.Pending)
                throw new BusinessException("任务状态不允许加签");

            // 调用工作流引擎加签
            var newTask = await _workflowEngine.AddSignAsync(taskId.ToString(), approverId, comment);

            // 记录加签日志
            await _approvalLogService.LogAsync(new ApprovalLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = _tenantContext.TenantId ?? string.Empty,
                ApprovalInstanceId = task.ApprovalInstanceId,
                TaskId = taskId,
                LogType = ApprovalLogType.AdditionalSign,
                OperatorId = _currentUser.Id?.ToString() ?? "system",
                OperatorName = _currentUser.UserName ?? "system",
                OperationTime = DateTime.UtcNow,
                Content = $"加签给 {approverId}: {comment}",
                IpAddress = _clientIpService.GetClientIp(_httpContextAccessor),
                UserAgent = _httpContextAccessor.GetUserAgent()
            });

            _logger.LogInformation("加签成功: 原任务ID={TaskId}, 新任务ID={NewTaskId}", taskId, newTask.Id);
            return newTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加签失败: 任务ID={TaskId}", taskId);
            throw;
        }
    }

    /// <summary>
    /// 转交任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="toUserId">接收人ID</param>
    /// <param name="comment">转交理由</param>
    /// <returns>转交结果</returns>
    public async Task<ApprovalTask> TransferTaskAsync(long taskId, string toUserId, string comment = "")
    {
        try
        {
            _logger.LogInformation("开始转交任务: 任务ID={TaskId}, 接收人={ToUserId}", taskId, toUserId);

            var task = await _context.ApprovalTasks
                .Include(x => x.ApprovalInstance)
                .FirstOrDefaultAsync(x => x.Id == taskId);

            if (task == null)
                throw new BusinessException("审批任务不存在");

            if (task.ApproverId != _currentUser.Id?.ToString())
                throw new BusinessException("无权转交此任务");

            if (task.Status != ApprovalTaskStatus.Pending)
                throw new BusinessException("任务状态不允许转交");

            // 调用工作流引擎转交任务
            var newTask = await _workflowEngine.TransferTaskAsync(
                taskId.ToString(),
                _currentUser.Id?.ToString() ?? "system",
                toUserId,
                comment);

            // 记录转交日志
            await _approvalLogService.LogAsync(new ApprovalLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = _tenantContext.TenantId ?? string.Empty,
                ApprovalInstanceId = task.ApprovalInstanceId,
                TaskId = taskId,
                LogType = ApprovalLogType.Transfer,
                OperatorId = _currentUser.Id?.ToString() ?? "system",
                OperatorName = _currentUser.UserName ?? "system",
                OperationTime = DateTime.UtcNow,
                Content = $"转交给 {toUserId}: {comment}",
                IpAddress = _clientIpService.GetClientIp(_httpContextAccessor),
                UserAgent = _httpContextAccessor.GetUserAgent()
            });

            _logger.LogInformation("任务转交成功: 原任务ID={TaskId}, 新任务ID={NewTaskId}", taskId, newTask.Id);
            return newTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转交任务失败: 任务ID={TaskId}", taskId);
            throw;
        }
    }

    /// <summary>
    /// 构建查询表达式（私有方法，用于内部查询构建）
    /// </summary>
    /// <param name="approverId">审批人ID</param>
    /// <param name="status">任务状态</param>
    /// <returns>查询表达式</returns>
    private Expression<Func<ApprovalTask, bool>> BuildQueryExpression(string? approverId = null, ApprovalTaskStatus? status = null)
    {
        var predicate = PredicateBuilder.New<ApprovalTask>(true);

        if (!string.IsNullOrEmpty(approverId))
        {
            predicate = predicate.And(x => x.ApproverId == approverId);
        }

        if (status.HasValue)
        {
            predicate = predicate.And(x => x.Status == status.Value);
        }

        return predicate;
    }
}
