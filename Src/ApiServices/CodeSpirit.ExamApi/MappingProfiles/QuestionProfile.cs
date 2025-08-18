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
            .ForMember(dest => dest.KnowledgePoints, opt => opt.MapFrom(src =>  src.KnowledgePoints))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Tags) 
                    ? JsonSerializer.Deserialize<List<string>>(src.Tags, new JsonSerializerOptions()) 
                    : null));

        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags != null && src.Tags.Any() ? JsonSerializer.Serialize(src.Tags, new JsonSerializerOptions()) : null));
        CreateMap<UpdateQuestionDto, Question>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags != null && src.Tags.Any() ? JsonSerializer.Serialize(src.Tags, new JsonSerializerOptions()) : null));

        // 添加 PageList 映射配置
        CreateMap<PageList<Question>, PageList<QuestionDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                src.Items.Select(q => new QuestionDto
                {
                    CategoryName = q.Category.Name,
                    KnowledgePoints = q.KnowledgePoints,
                    Tags = !string.IsNullOrEmpty(q.Tags)
                        ? JsonSerializer.Deserialize<List<string>>(q.Tags, new JsonSerializerOptions())
                        : null,
                }).ToList()
            ));
    }
}