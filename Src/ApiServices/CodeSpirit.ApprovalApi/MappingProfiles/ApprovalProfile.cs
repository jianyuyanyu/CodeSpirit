using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;

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
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

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

        // 审批日志映射
        CreateMap<ApprovalLog, ApprovalLogDto>()
            .ForMember(dest => dest.LogType, opt => opt.MapFrom(src => src.LogType.ToString()))
            .ForMember(dest => dest.Result, opt => opt.MapFrom(src => src.Result.HasValue ? src.Result.Value.ToString() : null));
    }
}
