using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 试卷映射配置
/// </summary>
public class ExamPaperProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamPaperProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            ExamPaper, 
            ExamPaperDto, 
            long, 
            CreateExamPaperDto, 
            UpdateExamPaperDto, 
            CreateExamPaperDto>();

        // 试卷题目映射
        CreateMap<ExamPaperQuestion, ExamPaperQuestionDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.QuestionVersion.Content))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Question.Type))
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionVersion.Options))
            .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.QuestionVersion.CorrectAnswer))
            .ForMember(dest => dest.Analysis, opt => opt.MapFrom(src => src.QuestionVersion.Analysis));

        // 创建试卷题目映射
        CreateMap<CreateExamPaperQuestionDto, ExamPaperQuestion>();
    }
} 