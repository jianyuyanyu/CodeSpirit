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
    }

    /// <summary>
    /// 创建题目选项映射
    /// </summary>
    private void CreateQuestionOptionMaps()
    {
        // QuestionOption实体到QuestionOptionDto的映射
        CreateMap<QuestionOption, QuestionOptionDto>();
    }
}
