using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.WrongQuestion;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 错题映射配置
/// </summary>
public class WrongQuestionProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public WrongQuestionProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            WrongQuestion, 
            WrongQuestionDto, 
            long, 
            CreateWrongQuestionDto, 
            UpdateWrongQuestionDto,
            CreateWrongQuestionDto>();

        // 自定义映射
        CreateMap<WrongQuestion, WrongQuestionDto>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.Name))
            .ForMember(dest => dest.QuestionContent, opt => opt.MapFrom(src => src.Question.Content))
            .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Question.Type))
            .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.Question.CorrectAnswer));
    }
} 