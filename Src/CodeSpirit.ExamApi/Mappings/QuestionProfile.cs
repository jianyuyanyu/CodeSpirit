using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using System.Text.Json;

/// <summary>
/// 题目AutoMapper配置
/// </summary>
public class QuestionProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionProfile()
    {
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.KnowledgePoints, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.KnowledgePoints) 
                    ? JsonSerializer.Deserialize<List<string>>(src.KnowledgePoints, new JsonSerializerOptions()) 
                    : null))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Tags) 
                    ? JsonSerializer.Deserialize<List<string>>(src.Tags, new JsonSerializerOptions()) 
                    : null));

        CreateMap<CreateQuestionDto, Question>();
        CreateMap<UpdateQuestionDto, Question>();
        CreateMap<QuestionVersion, QuestionVersionDto>();
    }
} 