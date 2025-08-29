using AutoMapper;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;

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
        CreateGeneratedQuestionMaps();
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

    /// <summary>
    /// 创建生成题目映射
    /// </summary>
    private void CreateGeneratedQuestionMaps()
    {
        // GeneratedQuestionDto到CreateQuestionDto的映射
        CreateMap<GeneratedQuestionDto, CreateQuestionDto>()
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore()) // 在外部设置
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => ParseQuestionType(src.Type)))
            .ForMember(dest => dest.LLMGenerated, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Validation, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore())
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options));

        // GeneratedQuestionOptionDto到CreateQuestionOptionDto的映射
        CreateMap<GeneratedQuestionOptionDto, CreateQuestionOptionDto>()
            .ForMember(dest => dest.IsOther, opt => opt.MapFrom(src => false)); // LLM生成的选项默认不是"其他"选项
    }

    /// <summary>
    /// 解析题目类型字符串为枚举
    /// </summary>
    /// <param name="typeString">题目类型字符串</param>
    /// <returns>题目类型枚举</returns>
    private static QuestionType ParseQuestionType(string typeString)
    {
        if (Enum.TryParse<QuestionType>(typeString, true, out var result))
        {
            return result;
        }
        
        // 默认返回Text类型
        return QuestionType.Text;
    }
}
