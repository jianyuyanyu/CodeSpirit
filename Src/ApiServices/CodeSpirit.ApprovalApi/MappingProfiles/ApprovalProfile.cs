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
            .ForMember(dest => dest.Nodes, opt => opt.Ignore())
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
            customPrompt = dto.CustomPrompt,
            //categoryId = dto.CategoryId
        };
        
        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }
}
