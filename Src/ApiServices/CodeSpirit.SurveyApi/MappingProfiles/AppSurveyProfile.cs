using AutoMapper;
using CodeSpirit.SurveyApi.Dtos.App;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// App端问卷映射配置
/// </summary>
public class AppSurveyProfile : Profile
{
    /// <summary>
    /// 初始化App端问卷映射配置
    /// </summary>
    public AppSurveyProfile()
    {
        // Survey -> AppSurveyDto
        CreateMap<Survey, AppSurveyDto>()
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.EstimatedMinutes, opt => opt.Ignore()) // 在控制器中计算
            .ForMember(dest => dest.IsExpired, opt => opt.Ignore()) // 计算属性
            .ForMember(dest => dest.CanParticipate, opt => opt.Ignore()); // 计算属性

        // Survey -> AppSurveyDetailDto
        CreateMap<Survey, AppSurveyDetailDto>()
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions.OrderBy(q => q.OrderIndex)))
            .ForMember(dest => dest.EstimatedMinutes, opt => opt.Ignore()) // 在控制器中计算
            .ForMember(dest => dest.IsExpired, opt => opt.Ignore()) // 计算属性
            .ForMember(dest => dest.CanParticipate, opt => opt.Ignore()); // 计算属性

        // Question -> AppQuestionDto
        CreateMap<Question, AppQuestionDto>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options.OrderBy(o => o.OrderIndex)));

        // QuestionOption -> AppQuestionOptionDto
        CreateMap<QuestionOption, AppQuestionOptionDto>();

        // SurveyResponse -> SubmitSurveyResponseDto
        CreateMap<SurveyResponse, SubmitSurveyResponseDto>()
            .ForMember(dest => dest.ResponseId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.CompletedAt ?? src.CreatedAt))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)));
    }

    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    /// <param name="status">回答状态</param>
    /// <returns>状态显示名称</returns>
    private static string GetStatusDisplayName(Models.Enums.ResponseStatus status)
    {
        return status switch
        {
            Models.Enums.ResponseStatus.InProgress => "进行中",
            Models.Enums.ResponseStatus.Completed => "已完成",
            Models.Enums.ResponseStatus.Abandoned => "已放弃",
            _ => "未知状态"
        };
    }
}
