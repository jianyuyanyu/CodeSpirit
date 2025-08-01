using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Services.Interfaces;
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

        // 试卷实体到DTO的自定义映射
        CreateMap<ExamPaper, ExamPaperDto>()
            .ForMember(dest => dest.ConversionDescription, opt => opt.MapFrom(src =>
                !src.EnableScoreConversion || !src.ConversionTargetFullScore.HasValue || !src.ConversionRatio.HasValue
                    ? string.Empty
                    : $"成绩换算：{src.TotalScore}分制 → {src.ConversionTargetFullScore.Value}分制，" +
                      $"换算比例：{src.ConversionRatio.Value:F4}，" +
                      $"及格分：{src.OriginalPassScore ?? src.PassScore} → {src.PassScore}，" +
                      $"小数保留：{src.ConversionDecimalPlaces}位。" +
                      $"换算公式：换算后成绩 = 原始成绩 × {src.ConversionRatio.Value:F4}（保留{src.ConversionDecimalPlaces}位小数）"
            ));

        // 试卷题目映射
        CreateMap<ExamPaperQuestion, ExamPaperQuestionDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.QuestionVersion.Content))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Question.Type))
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionVersion.Options))
            .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.QuestionVersion.CorrectAnswer))
            .ForMember(dest => dest.Analysis, opt => opt.MapFrom(src => src.QuestionVersion.Analysis));

        // 创建试卷题目映射
        CreateMap<CreateExamPaperQuestionDto, ExamPaperQuestion>();

        // 题目版本映射考题
        CreateMap<QuestionVersion, ExamPaperQuestion>()
            .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
            .ForMember(dest => dest.QuestionVersionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.DefaultScore))
            .ForMember(dest => dest.QuestionVersion, opt => opt.Ignore())
            ;
    }
} 