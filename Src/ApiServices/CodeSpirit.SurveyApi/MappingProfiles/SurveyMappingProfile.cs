using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// 问卷映射配置
/// </summary>
public class SurveyMappingProfile : Profile
{
    /// <summary>
    /// 初始化问卷映射配置
    /// </summary>
    public SurveyMappingProfile()
    {
        CreateSurveyMaps();
    }

    /// <summary>
    /// 创建问卷映射
    /// </summary>
    private void CreateSurveyMaps()
    {
        // Survey实体到SurveyDto的映射
        CreateMap<Survey, SurveyDto>()
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.ResponseCount, opt => opt.MapFrom(src => src.Responses.Count))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.AccessCode, opt => opt.MapFrom(src => src.PublicAccessCode))
            .ForMember(dest => dest.EstimatedMinutes, opt => opt.MapFrom(src => 5)); // 默认5分钟，后续可以从设置中获取

        // PageList泛型映射配置
        CreateMap<PageList<Survey>, PageList<SurveyDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total));

        // CreateSurveyDto到Survey实体的映射
        CreateMap<CreateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Models.Enums.SurveyStatus.Draft))
            .ForMember(dest => dest.PublishedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.Drafts, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // UpdateSurveyDto到Survey实体的映射
        CreateMap<UpdateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.PublishedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LLMPrompt, opt => opt.Ignore())
            .ForMember(dest => dest.LLMRawOutput, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.Drafts, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
    }
}
