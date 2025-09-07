using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.SurveyApi.Dtos.SurveyCategory;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// 问卷分类映射配置
/// </summary>
public class SurveyCategoryProfile : Profile
{
    /// <summary>
    /// 初始化问卷分类映射配置
    /// </summary>
    public SurveyCategoryProfile()
    {
        // 实体到DTO的映射
        CreateMap<SurveyCategory, SurveyCategoryDto>()
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.SurveyCount, opt => opt.MapFrom(src => src.Surveys.Count))
            .ForMember(dest => dest.Children, opt => opt.Ignore()); // Children 将通过树形构建方法设置

        // PageList映射配置
        CreateMap<PageList<SurveyCategory>, PageList<SurveyCategoryDto>>();

        // 创建DTO到实体的映射
        CreateMap<CreateSurveyCategoryDto, SurveyCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Surveys, opt => opt.Ignore());

        // 更新DTO到实体的映射
        CreateMap<UpdateSurveyCategoryDto, SurveyCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Surveys, opt => opt.Ignore());
    }
}
