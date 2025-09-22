using AutoMapper;
using CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.MappingProfiles;

/// <summary>
/// 流程分类映射配置
/// </summary>
public class WorkflowCategoryProfile : Profile
{
    /// <summary>
    /// 初始化流程分类映射配置
    /// </summary>
    public WorkflowCategoryProfile()
    {
        // WorkflowCategory -> WorkflowCategoryDto
        CreateMap<WorkflowCategory, WorkflowCategoryDto>()
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.WorkflowCount, opt => opt.MapFrom(src => src.WorkflowDefinitions.Count))
            .ForMember(dest => dest.Children, opt => opt.Ignore()); // 子分类通过服务层单独处理

        // CreateWorkflowCategoryDto -> WorkflowCategory
        CreateMap<CreateWorkflowCategoryDto, WorkflowCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowDefinitions, opt => opt.Ignore())
            .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => 0)); // 默认排序为0

        // UpdateWorkflowCategoryDto -> WorkflowCategory
        CreateMap<UpdateWorkflowCategoryDto, WorkflowCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowDefinitions, opt => opt.Ignore());
    }
}
