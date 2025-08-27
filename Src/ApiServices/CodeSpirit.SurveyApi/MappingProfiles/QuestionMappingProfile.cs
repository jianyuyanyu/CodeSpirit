using AutoMapper;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// 题目映射配置
/// </summary>
public class QuestionMappingProfile : Profile
{
    /// <summary>
    /// 初始化题目映射配置
    /// </summary>
    public QuestionMappingProfile()
    {
        CreateQuestionMaps();
        CreateQuestionOptionMaps();
    }

    /// <summary>
    /// 创建题目映射
    /// </summary>
    private void CreateQuestionMaps()
    {
        // Question实体到QuestionDto的映射
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options.OrderBy(o => o.OrderIndex)));

        // CreateQuestionDto到Question实体的映射
        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Options, opt => opt.Ignore()) // 选项单独处理
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // UpdateQuestionDto到Question实体的映射
        CreateMap<UpdateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Options, opt => opt.Ignore()) // 选项单独处理
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
    }

    /// <summary>
    /// 创建题目选项映射
    /// </summary>
    private void CreateQuestionOptionMaps()
    {
        // QuestionOption实体到QuestionOptionDto的映射
        CreateMap<QuestionOption, QuestionOptionDto>();

        // CreateQuestionOptionDto到QuestionOption实体的映射
        CreateMap<CreateQuestionOptionDto, QuestionOption>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.QuestionId, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore());

        // UpdateQuestionOptionDto到QuestionOption实体的映射
        CreateMap<UpdateQuestionOptionDto, QuestionOption>()
            .ForMember(dest => dest.QuestionId, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore());
    }
}
