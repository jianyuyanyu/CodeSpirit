using AutoMapper;
using CodeSpirit.SurveyApi.Dtos.Response;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// 问卷回答映射配置
/// </summary>
public class ResponseProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ResponseProfile()
    {
        // SurveyResponse -> ResponseDto
        CreateMap<SurveyResponse, ResponseDto>()
            .ForMember(dest => dest.SurveyTitle, opt => opt.MapFrom(src => src.Survey != null ? src.Survey.Title : null))
            .ForMember(dest => dest.DurationMinutes, opt => opt.Ignore()); // 在服务层计算

        // SurveyResponse -> ResponseDetailDto
        CreateMap<SurveyResponse, ResponseDetailDto>()
            .ForMember(dest => dest.SurveyTitle, opt => opt.MapFrom(src => src.Survey != null ? src.Survey.Title : null))
            .ForMember(dest => dest.SurveyDescription, opt => opt.MapFrom(src => src.Survey != null ? src.Survey.Description : null))
            .ForMember(dest => dest.DurationMinutes, opt => opt.Ignore()) // 在服务层计算
            .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.Answers));

        // ResponseAnswer -> ResponseAnswerDto
        CreateMap<ResponseAnswer, ResponseAnswerDto>()
            .ForMember(dest => dest.QuestionTitle, opt => opt.MapFrom(src => src.Question != null ? src.Question.Title : null))
            .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Question != null ? src.Question.Type.ToString() : null));
    }
}
