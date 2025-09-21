using AutoMapper;
using Newtonsoft.Json;
using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ApprovalApi.MappingProfiles;

/// <summary>
/// 审批模块映射配置
/// </summary>
public class ApprovalProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ApprovalProfile()
    {
        // 配置审批任务基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            ApprovalTask, 
            ApprovalTaskDto, 
            long, 
            ApprovalTaskDto, 
            ApprovalTaskDto, 
            ApprovalTaskQueryDto>();

        // 配置审批实例基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            ApprovalInstance, 
            ApprovalInstanceDto, 
            long, 
            StartApprovalDto, 
            StartApprovalDto, 
            ApprovalInstanceQueryDto>();

        // 配置工作流定义基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            WorkflowDefinition, 
            WorkflowDefinitionDto, 
            long, 
            CreateWorkflowDefinitionDto, 
            UpdateWorkflowDefinitionDto, 
            WorkflowDefinitionQueryDto>();

        // 配置工作流节点基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            WorkflowNode, 
            WorkflowNodeDto, 
            long, 
            CreateWorkflowNodeDto, 
            UpdateWorkflowNodeDto, 
            WorkflowNodeQueryDto>();

        // 审批实例映射
        CreateMap<ApprovalInstance, ApprovalInstanceDto>()
            .ForMember(dest => dest.WorkflowName, opt => opt.MapFrom(src => src.WorkflowDefinition.Name));
        
        CreateMap<StartApprovalDto, ApprovalInstance>()
            .ForMember(dest => dest.BusinessData, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.BusinessData)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        CreateMap<ApprovalInstance, ApprovalInstanceDetailDto>()
            .ForMember(dest => dest.WorkflowName, opt => opt.MapFrom(src => src.WorkflowDefinition.Name))
            .ForMember(dest => dest.Tasks, opt => opt.MapFrom(src => src.Tasks))
            .ForMember(dest => dest.Logs, opt => opt.Ignore()); // 日志单独获取

        // 审批任务映射
        CreateMap<ApprovalTask, ApprovalTaskDto>()
            .ForMember(dest => dest.ApprovalTitle, opt => opt.MapFrom(src => src.ApprovalInstance.Title))
            .ForMember(dest => dest.NodeName, opt => opt.Ignore()) // 需要从工作流节点获取
            .ForMember(dest => dest.EntityType, opt => opt.MapFrom(src => src.ApprovalInstance.EntityType))
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.ApprovalInstance.EntityId));

        CreateMap<ProcessApprovalTaskDto, ApprovalTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessedTime, opt => opt.Ignore());

        // 工作流定义映射
        CreateMap<WorkflowDefinition, WorkflowDefinitionDto>();
        
        CreateMap<CreateWorkflowDefinitionDto, WorkflowDefinition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Nodes, opt => opt.Ignore()) // 节点通过WorkflowNodeSchema单独处理
            // 忽略AI填充相关的辅助字段，这些字段不需要映射到实体
            .ForMember(dest => dest.Configuration, opt => opt.MapFrom(src => 
                string.IsNullOrEmpty(src.Configuration) ? 
                CreateDefaultConfiguration(src) : 
                src.Configuration))
            .AfterMap((src, dest) =>
            {
                // 如果没有提供FormSchema，可以在这里设置默认值或进行后续处理
                if (string.IsNullOrEmpty(dest.FormSchema) && !string.IsNullOrEmpty(src.FormSchema))
                {
                    dest.FormSchema = src.FormSchema;
                }
                
                // 处理WorkflowNodeSchema - 创建节点实体
                if (!string.IsNullOrEmpty(src.WorkflowNodeSchema))
                {
                    try
                    {
                        var nodeSchemas = JsonConvert.DeserializeObject<List<WorkflowNodeSchema>>(src.WorkflowNodeSchema);
                        if (nodeSchemas != null && nodeSchemas.Any())
                        {
                            dest.Nodes = CreateWorkflowNodesFromSchema(nodeSchemas, dest.TenantId ?? string.Empty);
                        }
                    }
                    catch (JsonException ex)
                    {
                        // 记录错误但不阻止工作流创建，节点可以后续手动添加
                        System.Diagnostics.Debug.WriteLine($"解析WorkflowNodeSchema失败: {ex.Message}");
                    }
                }
            });

        CreateMap<UpdateWorkflowDefinitionDto, WorkflowDefinition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<WorkflowDefinition, WorkflowDefinitionDetailDto>()
            .ForMember(dest => dest.Nodes, opt => opt.MapFrom(src => src.Nodes));

        // 工作流节点映射
        CreateMap<WorkflowNode, WorkflowNodeDto>()
            .ForMember(dest => dest.NodeType, opt => opt.MapFrom(src => src.NodeType.ToString()))
            .ForMember(dest => dest.ApprovalMode, opt => opt.MapFrom(src => src.ApprovalMode.ToString()))
            .ForMember(dest => dest.Approvers, opt => opt.MapFrom(src => src.Approvers))
            .ForMember(dest => dest.Conditions, opt => opt.MapFrom(src => src.Conditions));

        CreateMap<WorkflowNodeApprover, WorkflowNodeApproverDto>()
            .ForMember(dest => dest.ApproverType, opt => opt.MapFrom(src => src.ApproverType.ToString()));

        CreateMap<WorkflowNodeCondition, WorkflowNodeConditionDto>();

        // 创建工作流节点映射
        CreateMap<CreateWorkflowNodeDto, WorkflowNode>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowDefinition, opt => opt.Ignore())
            .ForMember(dest => dest.Approvers, opt => opt.Ignore())
            .ForMember(dest => dest.Conditions, opt => opt.Ignore());

        CreateMap<UpdateWorkflowNodeDto, WorkflowNode>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowDefinitionId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowDefinition, opt => opt.Ignore())
            .ForMember(dest => dest.Approvers, opt => opt.Ignore())
            .ForMember(dest => dest.Conditions, opt => opt.Ignore());

        // 工作流节点审批人映射
        CreateMap<CreateWorkflowNodeApproverDto, WorkflowNodeApprover>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowNodeId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowNode, opt => opt.Ignore());

        // 工作流节点条件映射
        CreateMap<CreateWorkflowNodeConditionDto, WorkflowNodeCondition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowNodeId, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowNode, opt => opt.Ignore());

        // 审批日志映射
        CreateMap<ApprovalLog, ApprovalLogDto>()
            .ForMember(dest => dest.LogType, opt => opt.MapFrom(src => src.LogType.ToString()))
            .ForMember(dest => dest.Result, opt => opt.MapFrom(src => src.Result.HasValue ? src.Result.Value.ToString() : null));
    }

    /// <summary>
    /// 创建默认工作流配置
    /// </summary>
    /// <param name="dto">创建工作流DTO</param>
    /// <returns>默认配置JSON</returns>
    private static string CreateDefaultConfiguration(CreateWorkflowDefinitionDto dto)
    {
        var config = new
        {
            timeout = 72, // 默认72小时超时
            autoReminder = true,
            reminderInterval = 24, // 24小时提醒间隔
            workflowType = dto.WorkflowType ?? "通用审批",
            businessScenario = dto.BusinessScenario ?? "通用业务场景",
            expectedApprovalLevels = dto.ExpectedApprovalLevels,
            requireConditionalBranch = dto.RequireConditionalBranch,
            conditionalBranchDescription = dto.ConditionalBranchDescription,
            approvalRoles = dto.ApprovalRoles,
            customPrompt = dto.CustomPrompt,
            // 工作流节点Schema不包含在配置中，因为它将用于创建实际的WorkflowNode实体
            //categoryId = dto.CategoryId
        };
        
        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }

    /// <summary>
    /// 根据节点Schema创建工作流节点实体
    /// </summary>
    /// <param name="nodeSchemas">节点Schema列表</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>工作流节点实体集合</returns>
    private static ICollection<WorkflowNode> CreateWorkflowNodesFromSchema(List<WorkflowNodeSchema> nodeSchemas, string tenantId)
    {
        var nodes = new List<WorkflowNode>();
        
        foreach (var schema in nodeSchemas)
        {
            var node = new WorkflowNode
            {
                TenantId = tenantId,
                Name = schema.Name ?? "未命名节点",
                NodeType = ParseNodeType(schema.NodeType),
                ApprovalMode = ParseApprovalMode(schema.ApprovalMode),
                Configuration = schema.Configuration ?? "{}"
            };

            // 创建审批人配置
            if (schema.Approvers != null && schema.Approvers.Any())
            {
                node.Approvers = schema.Approvers.Select(a => new WorkflowNodeApprover
                {
                    TenantId = tenantId,
                    ApproverType = ParseApproverType(a.ApproverType),
                    ApproverValue = a.ApproverValue ?? string.Empty,
                    ApproverName = a.ApproverName ?? string.Empty
                }).ToList();
            }

            // 创建条件配置
            if (schema.Conditions != null && schema.Conditions.Any())
            {
                node.Conditions = schema.Conditions.Select((c, index) => new WorkflowNodeCondition
                {
                    TenantId = tenantId,
                    Expression = c.Expression ?? string.Empty,
                    NextNodeName = c.NextNodeName ?? string.Empty,
                    Description = c.Description ?? string.Empty,
                    Order = index
                }).ToList();
            }

            nodes.Add(node);
        }

        return nodes;
    }

    /// <summary>
    /// 解析节点类型
    /// </summary>
    /// <param name="nodeType">节点类型字符串</param>
    /// <returns>节点类型枚举</returns>
    private static WorkflowNodeType ParseNodeType(string? nodeType)
    {
        if (string.IsNullOrEmpty(nodeType))
            return WorkflowNodeType.Approval;

        return nodeType.ToUpper() switch
        {
            "START" => WorkflowNodeType.Start,
            "APPROVAL" => WorkflowNodeType.Approval,
            "CONDITION" => WorkflowNodeType.Condition,
            "PARALLELGATEWAY" => WorkflowNodeType.ParallelGateway,
            "EXCLUSIVEGATEWAY" => WorkflowNodeType.ExclusiveGateway,
            "CARBONCOPY" => WorkflowNodeType.CarbonCopy,
            "END" => WorkflowNodeType.End,
            _ => WorkflowNodeType.Approval
        };
    }

    /// <summary>
    /// 解析审批模式
    /// </summary>
    /// <param name="approvalMode">审批模式字符串</param>
    /// <returns>审批模式枚举</returns>
    private static ApprovalMode ParseApprovalMode(string? approvalMode)
    {
        if (string.IsNullOrEmpty(approvalMode))
            return ApprovalMode.Sequential;

        return approvalMode.ToUpper() switch
        {
            "SEQUENTIAL" => ApprovalMode.Sequential,
            "PARALLEL" => ApprovalMode.Parallel,
            "COUNTERSIGN" => ApprovalMode.CounterSign,
            "ORSIGN" => ApprovalMode.OrSign,
            _ => ApprovalMode.Sequential
        };
    }

    /// <summary>
    /// 解析审批人类型
    /// </summary>
    /// <param name="approverType">审批人类型字符串</param>
    /// <returns>审批人类型枚举</returns>
    private static ApproverType ParseApproverType(string? approverType)
    {
        if (string.IsNullOrEmpty(approverType))
            return ApproverType.User;

        return approverType.ToUpper() switch
        {
            "USER" => ApproverType.User,
            "ROLE" => ApproverType.Role,
            "DEPARTMENT" => ApproverType.Department,
            "INITIATOR" => ApproverType.Initiator,
            "INITIATORSUPERIOR" => ApproverType.InitiatorSuperior,
            "EXPRESSION" => ApproverType.Expression,
            _ => ApproverType.User
        };
    }
}

/// <summary>
/// 工作流节点Schema
/// </summary>
public class WorkflowNodeSchema
{
    /// <summary>
    /// 节点名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    public string? ApprovalMode { get; set; }

    /// <summary>
    /// 节点配置
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// 审批人配置
    /// </summary>
    public List<WorkflowNodeApproverSchema>? Approvers { get; set; }

    /// <summary>
    /// 条件配置
    /// </summary>
    public List<WorkflowNodeConditionSchema>? Conditions { get; set; }
}

/// <summary>
/// 工作流节点审批人Schema
/// </summary>
public class WorkflowNodeApproverSchema
{
    /// <summary>
    /// 审批人类型
    /// </summary>
    public string? ApproverType { get; set; }

    /// <summary>
    /// 审批人值
    /// </summary>
    public string? ApproverValue { get; set; }

    /// <summary>
    /// 审批人名称
    /// </summary>
    public string? ApproverName { get; set; }
}

/// <summary>
/// 工作流节点条件Schema
/// </summary>
public class WorkflowNodeConditionSchema
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// 下一个节点名称
    /// </summary>
    public string? NextNodeName { get; set; }

    /// <summary>
    /// 条件描述
    /// </summary>
    public string? Description { get; set; }
}
