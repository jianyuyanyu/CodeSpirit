using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 练习记录映射配置
/// </summary>
public class PracticeRecordProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PracticeRecordProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            PracticeRecord,
            PracticeRecordDto,
            long,
            CreatePracticeRecordDto,
            UpdatePracticeRecordDto,
            PracticeRecordBatchImportDto>();
            
        // 自定义映射
        CreateMap<PracticeRecord, PracticeRecordDto>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.Name))
            .ForMember(dest => dest.QuestionContent, opt => opt.MapFrom(src => src.Question.Content))
            .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Question.Type))
            .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.Question.CorrectAnswer));
    }
} 