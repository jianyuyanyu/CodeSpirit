using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 工作流节点服务实现
/// </summary>
public class WorkflowNodeService : BaseCRUDService<WorkflowNode, WorkflowNodeDto, long, CreateWorkflowNodeDto, UpdateWorkflowNodeDto>, IWorkflowNodeService
{
    private readonly ApprovalDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WorkflowNodeService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <param name="logger">日志记录器</param>
    public WorkflowNodeService(
        IRepository<WorkflowNode> repository,
        IMapper mapper,
        ApprovalDbContext context,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ILogger<WorkflowNodeService> logger)
        : base(repository, mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// 根据工作流定义ID获取节点列表
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>节点列表</returns>
    public async Task<List<WorkflowNodeDto>> GetByWorkflowDefinitionIdAsync(long workflowDefinitionId)
    {
        var tenantId = _tenantContext.TenantId;
        
        var nodes = await _context.WorkflowNodes
            .Include(x => x.Approvers)
            .Include(x => x.Conditions)
            .Where(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return Mapper.Map<List<WorkflowNodeDto>>(nodes);
    }

    /// <summary>
    /// 批量创建工作流节点
    /// </summary>
    /// <param name="dto">批量创建DTO</param>
    /// <returns>创建的节点列表</returns>
    public async Task<List<WorkflowNodeDto>> BatchCreateAsync(BatchCreateWorkflowNodesDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        
        // 验证工作流定义是否存在
        var workflowExists = await _context.WorkflowDefinitions
            .AnyAsync(x => x.Id == dto.WorkflowDefinitionId && x.TenantId == tenantId);
        
        if (!workflowExists)
        {
            throw new BusinessException("工作流定义不存在");
        }

        // 先删除现有节点
        await DeleteByWorkflowDefinitionIdAsync(dto.WorkflowDefinitionId);

        var nodes = new List<WorkflowNode>();
        
        foreach (var nodeDto in dto.Nodes)
        {
            var node = Mapper.Map<WorkflowNode>(nodeDto);
            node.TenantId = tenantId ?? string.Empty;
            node.WorkflowDefinitionId = dto.WorkflowDefinitionId;
            
            // 设置审批人
            foreach (var approverDto in nodeDto.Approvers)
            {
                var approver = Mapper.Map<WorkflowNodeApprover>(approverDto);
                approver.TenantId = tenantId ?? string.Empty;
                node.Approvers.Add(approver);
            }
            
            // 设置条件
            foreach (var conditionDto in nodeDto.Conditions)
            {
                var condition = Mapper.Map<WorkflowNodeCondition>(conditionDto);
                condition.TenantId = tenantId ?? string.Empty;
                node.Conditions.Add(condition);
            }
            
            nodes.Add(node);
        }

        _context.WorkflowNodes.AddRange(nodes);
        await _context.SaveChangesAsync();

        return Mapper.Map<List<WorkflowNodeDto>>(nodes);
    }

    /// <summary>
    /// 保存流程设计
    /// </summary>
    /// <param name="dto">流程设计DTO</param>
    /// <returns>操作结果</returns>
    public async Task<bool> SaveProcessDesignAsync(WorkflowProcessDesignDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        
        // 验证工作流定义是否存在
        var workflow = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(x => x.Id == dto.WorkflowDefinitionId && x.TenantId == tenantId);
        
        if (workflow == null)
        {
            throw new BusinessException("工作流定义不存在");
        }

        // 验证节点配置
        var (isValid, errors) = await ValidateNodesAsync(dto.WorkflowDefinitionId, dto.Nodes);
        if (!isValid)
        {
            throw new BusinessException($"节点配置验证失败：{string.Join(", ", errors)}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 更新工作流配置
            workflow.Configuration = dto.ProcessConfig;
            workflow.UpdatedAt = DateTime.UtcNow;

            // 删除现有节点
            await DeleteByWorkflowDefinitionIdAsync(dto.WorkflowDefinitionId);

            // 创建新节点
            var batchDto = new BatchCreateWorkflowNodesDto
            {
                WorkflowDefinitionId = dto.WorkflowDefinitionId,
                Nodes = dto.Nodes
            };
            
            await BatchCreateAsync(batchDto);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 验证工作流节点配置
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="nodes">节点列表</param>
    /// <returns>验证结果</returns>
    public Task<(bool IsValid, List<string> Errors)> ValidateNodesAsync(long workflowDefinitionId, List<CreateWorkflowNodeDto> nodes)
    {
        var errors = new List<string>();

        // 检查是否有开始节点
        var startNodes = nodes.Where(x => x.NodeType == WorkflowNodeType.Start).ToList();
        if (startNodes.Count == 0)
        {
            errors.Add("必须有一个开始节点");
        }
        else if (startNodes.Count > 1)
        {
            errors.Add("只能有一个开始节点");
        }

        // 检查是否有结束节点
        var endNodes = nodes.Where(x => x.NodeType == WorkflowNodeType.End).ToList();
        if (endNodes.Count == 0)
        {
            errors.Add("必须有一个结束节点");
        }
        else if (endNodes.Count > 1)
        {
            errors.Add("只能有一个结束节点");
        }

        // 检查节点名称是否重复
        var duplicateNames = nodes.GroupBy(x => x.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicateNames.Any())
        {
            errors.Add($"节点名称重复：{string.Join(", ", duplicateNames)}");
        }

        // 检查审批节点是否有审批人
        var approvalNodes = nodes.Where(x => x.NodeType == WorkflowNodeType.Approval).ToList();
        foreach (var node in approvalNodes)
        {
            if (!node.Approvers.Any())
            {
                errors.Add($"审批节点 {node.Name} 必须设置审批人");
            }
        }

        // 检查条件节点是否有条件
        var conditionNodes = nodes.Where(x => x.NodeType == WorkflowNodeType.Condition).ToList();
        foreach (var node in conditionNodes)
        {
            if (!node.Conditions.Any())
            {
                errors.Add($"条件节点 {node.Name} 必须设置条件");
            }
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    /// <summary>
    /// 复制工作流节点
    /// </summary>
    /// <param name="sourceWorkflowDefinitionId">源工作流定义ID</param>
    /// <param name="targetWorkflowDefinitionId">目标工作流定义ID</param>
    /// <returns>复制的节点列表</returns>
    public async Task<List<WorkflowNodeDto>> CopyNodesAsync(long sourceWorkflowDefinitionId, long targetWorkflowDefinitionId)
    {
        var sourceNodes = await GetByWorkflowDefinitionIdAsync(sourceWorkflowDefinitionId);
        
        var createNodes = sourceNodes.Select(node => new CreateWorkflowNodeDto
        {
            WorkflowDefinitionId = targetWorkflowDefinitionId,
            Name = node.Name,
            NodeType = Enum.Parse<WorkflowNodeType>(node.NodeType),
            ApprovalMode = Enum.Parse<ApprovalMode>(node.ApprovalMode),
            Configuration = node.Configuration,
            Approvers = node.Approvers.Select(approver => new CreateWorkflowNodeApproverDto
            {
                ApproverType = Enum.Parse<ApproverType>(approver.ApproverType),
                ApproverValue = approver.ApproverValue,
                ApproverName = approver.ApproverName
            }).ToList(),
            Conditions = node.Conditions.Select(condition => new CreateWorkflowNodeConditionDto
            {
                Expression = condition.Expression,
                NextNodeName = condition.NextNodeName,
                Description = condition.Description
            }).ToList()
        }).ToList();

        var batchDto = new BatchCreateWorkflowNodesDto
        {
            WorkflowDefinitionId = targetWorkflowDefinitionId,
            Nodes = createNodes
        };

        return await BatchCreateAsync(batchDto);
    }

    /// <summary>
    /// 删除工作流定义的所有节点
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>操作结果</returns>
    public async Task<bool> DeleteByWorkflowDefinitionIdAsync(long workflowDefinitionId)
    {
        var tenantId = _tenantContext.TenantId;
        
        var nodes = await _context.WorkflowNodes
            .Where(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId)
            .ToListAsync();

        if (nodes.Any())
        {
            _context.WorkflowNodes.RemoveRange(nodes);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    /// <summary>
    /// 获取工作流预览数据
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>预览数据</returns>
    public async Task<WorkflowPreviewDto> GetWorkflowPreviewAsync(long workflowDefinitionId)
    {
        var tenantId = _tenantContext.TenantId;
        
        var workflow = await _context.WorkflowDefinitions
            .Include(x => x.Nodes)
                .ThenInclude(x => x.Approvers)
            .Include(x => x.Nodes)
                .ThenInclude(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == workflowDefinitionId && x.TenantId == tenantId);

        if (workflow == null)
        {
            throw new BusinessException("工作流定义不存在");
        }

        var nodes = workflow.Nodes.Select(node => new WorkflowNodePreviewDto
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.NodeType.ToString(),
            ApprovalMode = node.ApprovalMode.ToString(),
            Configuration = node.Configuration ?? "{}",
            Approvers = node.Approvers.Select(a => new WorkflowNodeApproverPreviewDto
            {
                Type = a.ApproverType.ToString(),
                Value = a.ApproverValue,
                Name = a.ApproverName
            }).ToList(),
            Conditions = node.Conditions.Select(c => new WorkflowNodeConditionPreviewDto
            {
                Expression = c.Expression,
                NextNodeName = c.NextNodeName,
                Description = c.Description
            }).ToList()
        }).ToList();

        return new WorkflowPreviewDto
        {
            Workflow = new WorkflowPreviewInfoDto
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Code = workflow.Code,
                Description = workflow.Description,
                Configuration = workflow.Configuration
            },
            Nodes = nodes
        };
    }

    /// <summary>
    /// 导入工作流节点
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="items">导入项列表</param>
    /// <returns>导入结果</returns>
    public async Task<(int SuccessCount, int ErrorCount, List<string> Errors)> ImportNodesAsync(
        long workflowDefinitionId, 
        List<WorkflowNodeBatchImportItemDto> items)
    {
        var errors = new List<string>();
        var successCount = 0;
        var errorCount = 0;

        var nodes = new List<CreateWorkflowNodeDto>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var rowIndex = i + 2; // Excel行号从2开始

            try
            {
                // 验证节点类型
                if (!Enum.TryParse<WorkflowNodeType>(item.NodeType, out var nodeType))
                {
                    errors.Add($"第{rowIndex}行：节点类型 '{item.NodeType}' 无效");
                    errorCount++;
                    continue;
                }

                // 验证审批模式
                var approvalMode = ApprovalMode.Sequential;
                if (!string.IsNullOrEmpty(item.ApprovalMode) && 
                    !Enum.TryParse<ApprovalMode>(item.ApprovalMode, out approvalMode))
                {
                    errors.Add($"第{rowIndex}行：审批模式 '{item.ApprovalMode}' 无效");
                    errorCount++;
                    continue;
                }

                var node = new CreateWorkflowNodeDto
                {
                    WorkflowDefinitionId = workflowDefinitionId,
                    Name = item.Name,
                    NodeType = nodeType,
                    ApprovalMode = approvalMode,
                    Configuration = item.Configuration
                };

                nodes.Add(node);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"第{rowIndex}行：{ex.Message}");
                errorCount++;
            }
        }

        // 如果有成功的数据，进行导入
        if (nodes.Any())
        {
            try
            {
                var batchDto = new BatchCreateWorkflowNodesDto
                {
                    WorkflowDefinitionId = workflowDefinitionId,
                    Nodes = nodes
                };

                await BatchCreateAsync(batchDto);
            }
            catch (Exception ex)
            {
                errors.Add($"批量导入失败：{ex.Message}");
                errorCount += successCount;
                successCount = 0;
            }
        }

        return (successCount, errorCount, errors);
    }

    /// <summary>
    /// 获取工作流可视化数据
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <returns>可视化数据</returns>
    public async Task<WorkflowVisualizationDto> GetWorkflowVisualizationAsync(long workflowDefinitionId)
    {
        var nodes = await GetByWorkflowDefinitionIdAsync(workflowDefinitionId);
        
        var flowNodes = new List<FlowChartNodeDto>();
        var flowEdges = new List<FlowChartEdgeDto>();
        
        // 节点布局计算
        var nodePositions = CalculateNodePositions(nodes);
        
        // 生成流程图节点
        foreach (var node in nodes)
        {
            var position = nodePositions.GetValueOrDefault(node.Id, new NodePositionDto { X = 100, Y = 100 });
            var flowNode = new FlowChartNodeDto
            {
                Id = node.Id.ToString(),
                Label = node.Name,
                Type = GetFlowChartNodeType(Enum.Parse<WorkflowNodeType>(node.NodeType)),
                Position = position,
                Data = new
                {
                    nodeType = node.NodeType,
                    approvalMode = node.ApprovalMode,
                    approvers = node.Approvers,
                    conditions = node.Conditions,
                    configuration = node.Configuration
                },
                Style = GetNodeStyle(Enum.Parse<WorkflowNodeType>(node.NodeType))
            };
            flowNodes.Add(flowNode);
        }
        
        // 生成连线（基于条件配置）
        foreach (var node in nodes)
        {
            foreach (var condition in node.Conditions)
            {
                var targetNode = nodes.FirstOrDefault(n => n.Name == condition.NextNodeName);
                if (targetNode != null)
                {
                    var edge = new FlowChartEdgeDto
                    {
                        Id = $"{node.Id}-{targetNode.Id}",
                        Source = node.Id.ToString(),
                        Target = targetNode.Id.ToString(),
                        Label = condition.Description,
                        Type = "smoothstep",
                        Style = new EdgeStyleDto()
                    };
                    flowEdges.Add(edge);
                }
            }
            
            // 对于非条件节点，添加默认连线到下一个节点
            if (!node.Conditions.Any() && Enum.Parse<WorkflowNodeType>(node.NodeType) != WorkflowNodeType.End)
            {
                var nextNode = nodes.FirstOrDefault(n => n.Id > node.Id);
                if (nextNode != null)
                {
                    var edge = new FlowChartEdgeDto
                    {
                        Id = $"{node.Id}-{nextNode.Id}",
                        Source = node.Id.ToString(),
                        Target = nextNode.Id.ToString(),
                        Type = "smoothstep",
                        Style = new EdgeStyleDto()
                    };
                    flowEdges.Add(edge);
                }
            }
        }
        
        return new WorkflowVisualizationDto
        {
            Nodes = flowNodes,
            Edges = flowEdges,
            Config = new FlowChartConfigDto()
        };
    }

    /// <summary>
    /// 高级验证工作流
    /// </summary>
    /// <param name="workflowDefinitionId">工作流定义ID</param>
    /// <param name="nodes">节点列表</param>
    /// <returns>验证结果</returns>
    public async Task<WorkflowValidationResultDto> AdvancedValidateWorkflowAsync(long workflowDefinitionId, List<CreateWorkflowNodeDto> nodes)
    {
        var result = new WorkflowValidationResultDto();
        var details = new List<ValidationDetailDto>();

        // 基础验证
        var (basicValid, basicErrors) = await ValidateNodesAsync(workflowDefinitionId, nodes);
        if (!basicValid)
        {
            result.Errors.AddRange(basicErrors);
            foreach (var error in basicErrors)
            {
                details.Add(new ValidationDetailDto
                {
                    ValidationType = "Basic",
                    Message = error,
                    Severity = ValidationSeverity.Error
                });
            }
        }

        // 高级验证：检查流程连通性
        var connectivityIssues = ValidateConnectivity(nodes);
        result.Warnings.AddRange(connectivityIssues);
        foreach (var warning in connectivityIssues)
        {
            details.Add(new ValidationDetailDto
            {
                ValidationType = "Connectivity",
                Message = warning,
                Severity = ValidationSeverity.Warning
            });
        }

        // 验证条件表达式
        var conditionIssues = ValidateConditions(nodes);
        result.Errors.AddRange(conditionIssues);
        foreach (var error in conditionIssues)
        {
            details.Add(new ValidationDetailDto
            {
                ValidationType = "Condition",
                Message = error,
                Severity = ValidationSeverity.Error
            });
        }

        // 验证审批人配置
        var approverIssues = ValidateApprovers(nodes);
        result.Warnings.AddRange(approverIssues);
        foreach (var warning in approverIssues)
        {
            details.Add(new ValidationDetailDto
            {
                ValidationType = "Approver",
                Message = warning,
                Severity = ValidationSeverity.Warning
            });
        }

        result.Details = details;
        result.IsValid = !result.Errors.Any();

        return result;
    }

    /// <summary>
    /// 获取工作流实例状态
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>实例状态</returns>
    public async Task<WorkflowInstanceStatusDto> GetWorkflowInstanceStatusAsync(long instanceId)
    {
        var tenantId = _tenantContext.TenantId;
        
        var instance = await _context.ApprovalInstances
            .Include(x => x.WorkflowDefinition)
                .ThenInclude(x => x.Nodes)
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == instanceId && x.TenantId == tenantId);

        if (instance == null)
        {
            throw new BusinessException("审批实例不存在");
        }

        var completedTasks = instance.Tasks.Where(t => t.Status == ApprovalTaskStatus.Completed).ToList();
        var totalNodes = instance.WorkflowDefinition.Nodes.Count;
        var completedNodesCount = completedTasks.Select(t => t.WorkflowNodeId).Distinct().Count();

        var status = new WorkflowInstanceStatusDto
        {
            InstanceId = instance.Id,
            CurrentNodeId = instance.CurrentNodeId,
            Status = instance.Status.ToString(),
            ProgressPercentage = totalNodes > 0 ? (decimal)completedNodesCount / totalNodes * 100 : 0,
            CompletedNodes = completedTasks.Select(t => new CompletedNodeDto
            {
                NodeId = t.WorkflowNodeId,
                NodeName = instance.WorkflowDefinition.Nodes.FirstOrDefault(n => n.Id == t.WorkflowNodeId)?.Name ?? "",
                CompletedAt = t.ProcessedTime ?? DateTime.MinValue,
                ProcessedBy = t.ApproverName ?? "",
                Result = t.Result?.ToString() ?? ""
            }).ToList()
        };

        if (instance.CurrentNodeId.HasValue)
        {
            var currentNode = instance.WorkflowDefinition.Nodes.FirstOrDefault(n => n.Id == instance.CurrentNodeId);
            status.CurrentNodeName = currentNode?.Name;
        }

        return status;
    }

    #region 私有辅助方法

    /// <summary>
    /// 计算节点位置
    /// </summary>
    /// <param name="nodes">节点列表</param>
    /// <returns>节点位置字典</returns>
    private Dictionary<long, NodePositionDto> CalculateNodePositions(List<WorkflowNodeDto> nodes)
    {
        var positions = new Dictionary<long, NodePositionDto>();
        var xOffset = 200;
        var yOffset = 80;
        var currentX = 100;
        var currentY = 100;

        for (int i = 0; i < nodes.Count; i++)
        {
            positions[nodes[i].Id] = new NodePositionDto
            {
                X = currentX,
                Y = currentY
            };

            // 简单的网格布局
            currentX += xOffset;
            if ((i + 1) % 4 == 0) // 每行4个节点
            {
                currentX = 100;
                currentY += yOffset;
            }
        }

        return positions;
    }

    /// <summary>
    /// 获取流程图节点类型
    /// </summary>
    /// <param name="nodeType">工作流节点类型</param>
    /// <returns>流程图节点类型</returns>
    private string GetFlowChartNodeType(WorkflowNodeType nodeType)
    {
        return nodeType switch
        {
            WorkflowNodeType.Start => "input",
            WorkflowNodeType.End => "output",
            WorkflowNodeType.Condition => "decision",
            WorkflowNodeType.ParallelGateway => "parallelGateway",
            WorkflowNodeType.ExclusiveGateway => "exclusiveGateway",
            _ => "default"
        };
    }

    /// <summary>
    /// 获取节点样式
    /// </summary>
    /// <param name="nodeType">节点类型</param>
    /// <returns>节点样式</returns>
    private NodeStyleDto GetNodeStyle(WorkflowNodeType nodeType)
    {
        return nodeType switch
        {
            WorkflowNodeType.Start => new NodeStyleDto
            {
                BackgroundColor = "#e8f5e8",
                BorderColor = "#52c41a",
                Color = "#135200"
            },
            WorkflowNodeType.End => new NodeStyleDto
            {
                BackgroundColor = "#ffe7e7",
                BorderColor = "#f5222d",
                Color = "#a8071a"
            },
            WorkflowNodeType.Approval => new NodeStyleDto
            {
                BackgroundColor = "#e6f7ff",
                BorderColor = "#1890ff",
                Color = "#003a8c"
            },
            WorkflowNodeType.Condition => new NodeStyleDto
            {
                BackgroundColor = "#fff7e6",
                BorderColor = "#fa8c16",
                Color = "#ad4e00"
            },
            _ => new NodeStyleDto()
        };
    }

    /// <summary>
    /// 验证流程连通性
    /// </summary>
    /// <param name="nodes">节点列表</param>
    /// <returns>连通性问题列表</returns>
    private List<string> ValidateConnectivity(List<CreateWorkflowNodeDto> nodes)
    {
        var issues = new List<string>();
        
        // 检查是否有孤立节点
        var nodeNames = nodes.Select(n => n.Name).ToHashSet();
        var referencedNodes = nodes.SelectMany(n => n.Conditions.Select(c => c.NextNodeName)).ToHashSet();
        
        var unreferencedNodes = nodeNames.Except(referencedNodes).Where(name => 
            !nodes.Any(n => n.Name == name && (n.NodeType == WorkflowNodeType.Start || n.NodeType == WorkflowNodeType.End))).ToList();
            
        if (unreferencedNodes.Any())
        {
            issues.Add($"存在未被引用的节点：{string.Join(", ", unreferencedNodes)}");
        }

        // 检查是否有引用不存在的节点
        var invalidReferences = referencedNodes.Except(nodeNames).ToList();
        if (invalidReferences.Any())
        {
            issues.Add($"引用了不存在的节点：{string.Join(", ", invalidReferences)}");
        }

        return issues;
    }

    /// <summary>
    /// 验证条件表达式
    /// </summary>
    /// <param name="nodes">节点列表</param>
    /// <returns>条件问题列表</returns>
    private List<string> ValidateConditions(List<CreateWorkflowNodeDto> nodes)
    {
        var issues = new List<string>();
        
        foreach (var node in nodes.Where(n => n.NodeType == WorkflowNodeType.Condition))
        {
            foreach (var condition in node.Conditions)
            {
                if (string.IsNullOrWhiteSpace(condition.Expression))
                {
                    issues.Add($"条件节点 {node.Name} 的条件表达式不能为空");
                }
                // 这里可以添加更复杂的表达式语法验证
            }
        }

        return issues;
    }

    /// <summary>
    /// 验证审批人配置
    /// </summary>
    /// <param name="nodes">节点列表</param>
    /// <returns>审批人问题列表</returns>
    private List<string> ValidateApprovers(List<CreateWorkflowNodeDto> nodes)
    {
        var issues = new List<string>();
        
        foreach (var node in nodes.Where(n => n.NodeType == WorkflowNodeType.Approval))
        {
            if (!node.Approvers.Any())
            {
                issues.Add($"审批节点 {node.Name} 缺少审批人配置");
            }
            
            foreach (var approver in node.Approvers)
            {
                if (string.IsNullOrWhiteSpace(approver.ApproverValue))
                {
                    issues.Add($"审批节点 {node.Name} 的审批人值不能为空");
                }
            }
        }

        return issues;
    }

    #endregion
}
