using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批实例服务实现
/// </summary>
public class ApprovalInstanceService : IApprovalInstanceService
{
    private readonly ApprovalDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IApprovalLogService _approvalLogService;
    private readonly IIntelligentApprovalService _intelligentApprovalService;
    private readonly ITenantAwareEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IClientIpService _clientIpService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApprovalInstanceService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApprovalInstanceService(
        ApprovalDbContext context,
        IMapper mapper,
        IWorkflowEngine workflowEngine,
        IApprovalLogService approvalLogService,
        IIntelligentApprovalService intelligentApprovalService,
        ITenantAwareEventBus eventBus,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IClientIpService clientIpService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApprovalInstanceService> logger)
    {
        _context = context;
        _mapper = mapper;
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
    /// 发起审批
    /// </summary>
    public async Task<ApprovalInstance> StartApprovalAsync(StartApprovalDto dto)
    {
        try
        {
            _logger.LogInformation("开始发起审批: 工作流={WorkflowCode}", dto.WorkflowCode);

            // 启动工作流
            var instance = await _workflowEngine.StartWorkflowAsync(
                dto.WorkflowCode,
                dto.EntityType,
                dto.EntityId,
                _currentUser.Id?.ToString() ?? "system",
                dto.Title,
                dto.BusinessData,
                _tenantContext.TenantId);

            // 记录审批日志
            await _approvalLogService.LogAsync(new ApprovalLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = _tenantContext.TenantId ?? string.Empty,
                ApprovalInstanceId = instance.Id,
                LogType = ApprovalLogType.Start,
                OperatorId = _currentUser.Id?.ToString() ?? "system",
                OperatorName = _currentUser.GetDisplayName(),
                OperationTime = DateTime.UtcNow,
                Content = $"发起审批: {dto.Title}",
                IpAddress = _clientIpService.GetClientIp(_httpContextAccessor),
                UserAgent = _httpContextAccessor.GetUserAgent()
            });

            _logger.LogInformation("审批发起成功: 实例ID={InstanceId}", instance.Id);
            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发起审批失败: 工作流={WorkflowCode}", dto.WorkflowCode);
            throw;
        }
    }

    /// <summary>
    /// 获取审批详情
    /// </summary>
    public async Task<ApprovalInstanceDetailDto?> GetDetailAsync(long id)
    {
        var instance = await _context.ApprovalInstances
            .Include(x => x.Tasks)
            .Include(x => x.WorkflowDefinition)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (instance == null)
            return null;

        var detail = _mapper.Map<ApprovalInstanceDetailDto>(instance);

        // 获取审批日志
        var logs = await _approvalLogService.GetLogsAsync(id.ToString());
        detail.Logs = logs;

        return detail;
    }

    /// <summary>
    /// 撤回审批
    /// </summary>
    public async Task<bool> WithdrawAsync(long id, string reason)
    {
        try
        {
            var instance = await _context.ApprovalInstances.FirstOrDefaultAsync(x => x.Id == id);
            if (instance == null)
                return false;

            // 检查是否可以撤回
            if (instance.ApplicantId != (_currentUser.Id?.ToString() ?? "system"))
                throw new BusinessException("只能撤回自己发起的审批");

            // 调用工作流引擎撤回
            var result = await _workflowEngine.WithdrawAsync(id.ToString(), _currentUser.Id?.ToString() ?? "system", reason);

            if (result)
            {
                _logger.LogInformation("审批撤回成功: 实例ID={InstanceId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤回审批失败: 实例ID={InstanceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据业务实体获取审批实例
    /// </summary>
    public async Task<List<ApprovalInstance>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _context.ApprovalInstances
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.ApplyTime)
            .ToListAsync();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageList<ApprovalInstanceDto>> GetPagedListAsync(ApprovalInstanceQueryDto query)
    {
        var queryable = _context.ApprovalInstances
            .Include(x => x.WorkflowDefinition)
            .AsQueryable();

        // 这里可以添加查询条件过滤

        var totalCount = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.ApplyTime)
            .Skip((query.Page - 1) * query.PerPage)
            .Take(query.PerPage)
            .ToListAsync();

        var dtos = _mapper.Map<List<ApprovalInstanceDto>>(items);

        return new PageList<ApprovalInstanceDto>
        {
            Items = dtos,
            Total = totalCount
        };
    }

    /// <summary>
    /// 获取审批实例
    /// </summary>
    public async Task<ApprovalInstanceDto?> GetAsync(long id)
    {
        var instance = await _context.ApprovalInstances
            .Include(x => x.WorkflowDefinition)
            .FirstOrDefaultAsync(x => x.Id == id);

        return instance != null ? _mapper.Map<ApprovalInstanceDto>(instance) : null;
    }

    /// <summary>
    /// 获取我发起的审批
    /// </summary>
    public async Task<PageList<ApprovalInstanceDto>> GetMyApplicationsAsync(string applicantId, int pageIndex = 1, int pageSize = 20)
    {
        var query = _context.ApprovalInstances
            .Include(x => x.WorkflowDefinition)
            .Where(x => x.ApplicantId == applicantId)
            .OrderByDescending(x => x.ApplyTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<ApprovalInstanceDto>>(items);

        return new PageList<ApprovalInstanceDto>
        {
            Items = dtos,
            Total = totalCount
        };
    }
}