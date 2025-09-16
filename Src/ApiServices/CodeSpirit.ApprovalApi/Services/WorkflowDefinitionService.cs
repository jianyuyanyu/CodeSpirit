using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流定义服务实现
/// </summary>
public class WorkflowDefinitionService : BaseCRUDService<WorkflowDefinition, WorkflowDefinitionDto, long, CreateWorkflowDefinitionDto, UpdateWorkflowDefinitionDto>, IWorkflowDefinitionService
{
    private readonly ApprovalDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WorkflowDefinitionService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="currentTenant">当前租户</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cache">内存缓存</param>
    public WorkflowDefinitionService(
        IRepository<WorkflowDefinition> repository,
        IMapper mapper,
        ApprovalDbContext context,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ILogger<WorkflowDefinitionService> logger,
        IMemoryCache cache)
        : base(repository, mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// 根据代码获取工作流定义
    /// </summary>
    /// <param name="code">工作流代码</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>工作流定义</returns>
    public async Task<WorkflowDefinition?> GetByCodeAsync(string code, string? tenantId = null)
    {
        tenantId ??= _tenantContext.TenantId;
        var cacheKey = $"workflow_definition_{tenantId}_{code}";

        if (_cache.TryGetValue(cacheKey, out WorkflowDefinition? cachedWorkflow))
        {
            return cachedWorkflow;
        }

        var workflow = await _context.WorkflowDefinitions
            .Include(x => x.Nodes)
            .ThenInclude(x => x.Approvers)
            .Include(x => x.Nodes)
            .ThenInclude(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Code == code && x.TenantId == tenantId && x.IsEnabled);

        if (workflow != null)
        {
            // 缓存30分钟
            _cache.Set(cacheKey, workflow, TimeSpan.FromMinutes(30));
        }

        return workflow;
    }

    /// <summary>
    /// 启用/禁用工作流
    /// </summary>
    /// <param name="id">工作流ID</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>操作结果</returns>
    public async Task<bool> SetEnabledAsync(long id, bool enabled)
    {
        try
        {
            var workflow = await Repository.GetByIdAsync(id);
            if (workflow == null)
                return false;

            workflow.IsEnabled = enabled;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.UpdatedBy = _currentUser.Id;

            await Repository.UpdateAsync(workflow);

            // 清除缓存
            ClearWorkflowCache(workflow.TenantId, workflow.Code);

            _logger.LogInformation("工作流状态更新: ID={Id}, 启用={Enabled}", id, enabled);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新工作流状态失败: ID={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 复制工作流定义
    /// </summary>
    /// <param name="sourceId">源工作流ID</param>
    /// <param name="newName">新工作流名称</param>
    /// <param name="newCode">新工作流代码</param>
    /// <returns>新工作流定义</returns>
    public async Task<WorkflowDefinition> CopyAsync(long sourceId, string newName, string newCode)
    {
        try
        {
            var sourceWorkflow = await _context.WorkflowDefinitions
                .Include(x => x.Nodes)
                .ThenInclude(x => x.Approvers)
                .Include(x => x.Nodes)
                .ThenInclude(x => x.Conditions)
                .FirstOrDefaultAsync(x => x.Id == sourceId);

            if (sourceWorkflow == null)
                throw new BusinessException("源工作流不存在");

            // 检查新代码是否已存在
            var existingWorkflow = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(x => x.Code == newCode && x.TenantId == _tenantContext.TenantId);
            if (existingWorkflow != null)
                throw new BusinessException("工作流代码已存在");

            // 创建新工作流
            var newWorkflow = new WorkflowDefinition
            {
                TenantId = _tenantContext.TenantId,
                Name = newName,
                Code = newCode,
                Description = $"复制自: {sourceWorkflow.Name}",
                Version = 1,
                IsEnabled = false, // 复制的工作流默认禁用
                Configuration = sourceWorkflow.Configuration,
                FormSchema = sourceWorkflow.FormSchema,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Id ?? 0
            };

            await Repository.AddAsync(newWorkflow);
            await _context.SaveChangesAsync();

            // 复制节点
            var nodeMapping = new Dictionary<long, long>();
            foreach (var sourceNode in sourceWorkflow.Nodes)
            {
                var newNode = new WorkflowNode
                {
                    WorkflowDefinitionId = newWorkflow.Id,
                    Name = sourceNode.Name,
                    NodeType = sourceNode.NodeType,
                    ApprovalMode = sourceNode.ApprovalMode,
                    Configuration = sourceNode.Configuration
                };

                _context.WorkflowNodes.Add(newNode);
                await _context.SaveChangesAsync();

                nodeMapping[sourceNode.Id] = newNode.Id;

                // 复制审批人
                foreach (var sourceApprover in sourceNode.Approvers)
                {
                    var newApprover = new WorkflowNodeApprover
                    {
                        WorkflowNodeId = newNode.Id,
                        ApproverType = sourceApprover.ApproverType,
                        ApproverValue = sourceApprover.ApproverValue,
                        ApproverName = sourceApprover.ApproverName
                    };

                    _context.WorkflowNodeApprovers.Add(newApprover);
                }

                // 复制条件
                foreach (var sourceCondition in sourceNode.Conditions)
                {
                    var newCondition = new WorkflowNodeCondition
                    {
                        WorkflowNodeId = newNode.Id,
                        Expression = sourceCondition.Expression,
                        NextNodeName = sourceCondition.NextNodeName,
                        Description = sourceCondition.Description
                    };

                    _context.WorkflowNodeConditions.Add(newCondition);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("工作流复制成功: 源ID={SourceId}, 新ID={NewId}, 新代码={NewCode}", 
                sourceId, newWorkflow.Id, newCode);

            return newWorkflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制工作流失败: 源ID={SourceId}", sourceId);
            throw;
        }
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <returns>创建的实体</returns>
    public override async Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionDto createDto)
    {
        // 检查代码是否已存在
        var existingWorkflow = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(x => x.Code == createDto.Code && x.TenantId == _tenantContext.TenantId);
        if (existingWorkflow != null)
            throw new BusinessException("工作流代码已存在");

        var entity = Mapper.Map<WorkflowDefinition>(createDto);
        entity.TenantId = _tenantContext.TenantId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = _currentUser.Id ?? 0;

        await Repository.AddAsync(entity);

        _logger.LogInformation("工作流定义创建成功: ID={Id}, 代码={Code}", entity.Id, entity.Code);
        return Mapper.Map<WorkflowDefinitionDto>(entity);
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="updateDto">更新DTO</param>
    /// <returns>更新的实体</returns>
    public override async Task<WorkflowDefinition> UpdateAsync(long id, UpdateWorkflowDefinitionDto updateDto)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
            throw new BusinessException("工作流定义不存在");

        // 如果修改了代码，检查新代码是否已存在
        if (updateDto.Code != entity.Code)
        {
            var existingWorkflow = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(x => x.Code == updateDto.Code && x.TenantId == _tenantContext.TenantId && x.Id != id);
            if (existingWorkflow != null)
                throw new BusinessException("工作流代码已存在");
        }

        Mapper.Map(updateDto, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.Id;
        entity.Version++; // 版本号递增

        await _context.SaveChangesAsync();

        // 清除缓存
        ClearWorkflowCache(entity.TenantId, entity.Code);

        _logger.LogInformation("工作流定义更新成功: ID={Id}, 代码={Code}", entity.Id, entity.Code);
        return entity;
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>删除结果</returns>
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
            return false;

        // 检查是否有正在使用的审批实例
        var hasActiveInstances = await _context.ApprovalInstances
            .AnyAsync(x => x.WorkflowDefinitionId == id && 
                          (x.Status == ApprovalStatus.Pending || x.Status == ApprovalStatus.InProgress));

        if (hasActiveInstances)
            throw new BusinessException("存在正在进行的审批实例，无法删除工作流定义");

        // 软删除
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.Id;

        await _context.SaveChangesAsync();

        // 清除缓存
        ClearWorkflowCache(entity.TenantId, entity.Code);

        _logger.LogInformation("工作流定义删除成功: ID={Id}, 代码={Code}", entity.Id, entity.Code);
        return true;
    }

    /// <summary>
    /// 构建查询表达式（私有方法，用于内部查询构建）
    /// </summary>
    /// <param name="query">查询DTO</param>
    /// <returns>查询表达式</returns>
    private Expression<Func<WorkflowDefinition, bool>> BuildQueryExpressionInternal(object? query)
    {
        var predicate = PredicateBuilder.New<WorkflowDefinition>(true);

        if (query is WorkflowDefinitionQueryDto queryDto)
        {
            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                predicate = predicate.And(x => x.Name.Contains(queryDto.Name));
            }

            if (!string.IsNullOrEmpty(queryDto.Code))
            {
                predicate = predicate.And(x => x.Code.Contains(queryDto.Code));
            }

            if (queryDto.IsEnabled.HasValue)
            {
                predicate = predicate.And(x => x.IsEnabled == queryDto.IsEnabled.Value);
            }

            if (queryDto.Version.HasValue)
            {
                predicate = predicate.And(x => x.Version == queryDto.Version.Value);
            }
        }

        return predicate;
    }

    /// <summary>
    /// 清除工作流缓存
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="code">工作流代码</param>
    private void ClearWorkflowCache(string tenantId, string code)
    {
        var cacheKey = $"workflow_definition_{tenantId}_{code}";
        _cache.Remove(cacheKey);
    }
}
