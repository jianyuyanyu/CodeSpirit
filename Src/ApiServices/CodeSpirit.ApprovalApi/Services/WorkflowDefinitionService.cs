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
    private readonly IRepository<ApprovalInstance> _instanceRepository;
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
    /// <param name="instanceRepository">审批实例仓储</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cache">内存缓存</param>
    public WorkflowDefinitionService(
        IRepository<WorkflowDefinition> repository,
        IMapper mapper,
        ApprovalDbContext context,
        IRepository<ApprovalInstance> instanceRepository,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ILogger<WorkflowDefinitionService> logger,
        IMemoryCache cache)
        : base(repository, mapper)
    {
        _context = context;
        _instanceRepository = instanceRepository;
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

        var workflow = await Repository.CreateQuery()
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
        workflow.UpdatedBy = _currentUser.Id ?? 0;

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
            var sourceWorkflow = await Repository.CreateQuery()
                .Include(x => x.Nodes)
                .ThenInclude(x => x.Approvers)
                .Include(x => x.Nodes)
                .ThenInclude(x => x.Conditions)
                .FirstOrDefaultAsync(x => x.Id == sourceId);

            if (sourceWorkflow == null)
                throw new BusinessException("源工作流不存在");

            // 检查新代码是否已存在
            var existingWorkflow = await Repository.CreateQuery()
                .FirstOrDefaultAsync(x => x.Code == newCode && x.TenantId == _tenantContext.TenantId);
            if (existingWorkflow != null)
                throw new BusinessException("工作流代码已存在");

            // 创建新工作流
            var newWorkflow = new WorkflowDefinition
            {
                TenantId = _tenantContext.TenantId ?? string.Empty,
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
        var existingWorkflow = await Repository.CreateQuery()
            .FirstOrDefaultAsync(x => x.Code == createDto.Code && x.TenantId == _tenantContext.TenantId);
        if (existingWorkflow != null)
            throw new BusinessException("工作流代码已存在");

        var entity = Mapper.Map<WorkflowDefinition>(createDto);
        entity.TenantId = _tenantContext.TenantId ?? string.Empty;
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
            var existingWorkflow = await Repository.CreateQuery()
                .FirstOrDefaultAsync(x => x.Code == updateDto.Code && x.TenantId == _tenantContext.TenantId && x.Id != id);
            if (existingWorkflow != null)
                throw new BusinessException("工作流代码已存在");
        }

        Mapper.Map(updateDto, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.Id ?? 0;
        entity.Version++; // 版本号递增

        await Repository.UpdateAsync(entity);

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
        var hasActiveInstances = await _instanceRepository.CreateQuery()
            .AnyAsync(x => x.WorkflowDefinitionId == id && 
                          (x.Status == ApprovalStatus.Pending || x.Status == ApprovalStatus.InProgress));

        if (hasActiveInstances)
            throw new BusinessException("存在正在进行的审批实例，无法删除工作流定义");

        // 软删除
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.Id ?? 0;

        await Repository.UpdateAsync(entity);

        // 清除缓存
        ClearWorkflowCache(entity.TenantId, entity.Code);

        _logger.LogInformation("工作流定义删除成功: ID={Id}, 代码={Code}", entity.Id, entity.Code);
        return true;
    }

    /// <summary>
    /// 构建查询表达式（重写基类方法）
    /// </summary>
    /// <param name="queryDto">查询DTO</param>
    /// <returns>查询表达式</returns>
    protected override Expression<Func<WorkflowDefinition, bool>>? BuildQueryExpression(object? queryDto)
    {
        return BuildQueryExpressionInternal(queryDto);
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

            if (queryDto.CategoryId.HasValue)
            {
                predicate = predicate.And(x => x.CategoryId == queryDto.CategoryId.Value);
            }
        }

        return predicate;
    }

    /// <summary>
    /// 快速保存工作流定义
    /// </summary>
    /// <param name="request">快速保存请求</param>
    /// <returns>操作结果</returns>
    public async Task QuickSaveWorkflowDefinitionsAsync(WorkflowDefinitionQuickSaveRequestDto request)
    {
        if (request?.Rows == null || !request.Rows.Any())
        {
            throw new AppServiceException(400, "请求数据无效或为空！");
        }

        // 获取需要更新的工作流定义ID列表
        List<long> workflowIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
        List<WorkflowDefinition> workflowsToUpdate = await GetWorkflowsByIdsAsync(workflowIdsToUpdate);
        if (workflowsToUpdate.Count != workflowIdsToUpdate.Count)
        {
            throw new AppServiceException(400, "部分工作流定义未找到!");
        }

        // 执行批量更新：更新 `rowsDiff` 中的变化字段
        foreach (WorkflowDefinitionDiffDto rowDiff in request.RowsDiff)
        {
            WorkflowDefinition workflow = workflowsToUpdate.FirstOrDefault(w => w.Id == rowDiff.Id);
            if (workflow != null)
            {
                // 更新名称
                if (!string.IsNullOrEmpty(rowDiff.Name))
                {
                    workflow.Name = rowDiff.Name;
                }

                // 更新代码（需要检查唯一性）
                if (!string.IsNullOrEmpty(rowDiff.Code) && rowDiff.Code != workflow.Code)
                {
                    var existingWorkflow = await Repository.CreateQuery()
                        .FirstOrDefaultAsync(x => x.Code == rowDiff.Code && x.TenantId == _tenantContext.TenantId && x.Id != workflow.Id);
                    if (existingWorkflow != null)
                        throw new AppServiceException(400, $"工作流代码 '{rowDiff.Code}' 已存在!");
                    
                    workflow.Code = rowDiff.Code;
                }

                // 更新描述
                if (rowDiff.Description != null)
                {
                    workflow.Description = rowDiff.Description;
                }

                // 更新启用状态
                if (rowDiff.IsEnabled.HasValue)
                {
                    workflow.IsEnabled = rowDiff.IsEnabled.Value;
                }

                // 更新配置
                if (rowDiff.Configuration != null)
                {
                    workflow.Configuration = rowDiff.Configuration;
                }

                // 更新表单Schema
                if (rowDiff.FormSchema != null)
                {
                    workflow.FormSchema = rowDiff.FormSchema;
                }

                // 设置更新信息
                workflow.UpdatedAt = DateTime.UtcNow;
                workflow.UpdatedBy = _currentUser.Id ?? 0;
                workflow.Version++; // 版本号递增

                // 清除缓存
                ClearWorkflowCache(workflow.TenantId, workflow.Code);
            }
        }

        // 批量保存更改
        await _context.SaveChangesAsync();

        _logger.LogInformation("工作流定义快速保存成功，更新了 {Count} 个工作流定义", request.RowsDiff.Count);
    }

    /// <summary>
    /// 根据ID列表获取工作流定义
    /// </summary>
    /// <param name="ids">ID列表</param>
    /// <returns>工作流定义列表</returns>
    private async Task<List<WorkflowDefinition>> GetWorkflowsByIdsAsync(List<long> ids)
    {
        var tenantId = _tenantContext.TenantId;
        
        var workflows = await Repository.CreateQuery()
            .Where(w => ids.Contains(w.Id) && w.TenantId == tenantId && !w.IsDeleted)
            .ToListAsync();

        // 如果没有找到任何工作流定义，返回空列表
        return workflows == null || !workflows.Any() ? [] : workflows;
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
