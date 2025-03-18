using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 考试记录映射配置
/// </summary>
public class ExamRecordProfile : Profile
{
    public ExamRecordProfile()
    {
        // 从 ExamRecord 到 ExamRecordDto 的映射
        CreateMap<ExamRecord, ExamRecordDto>()
            .ForMember(dest => dest.ExamName, opt => opt.MapFrom(src => src.ExamSetting.Name))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.Name));
            
        // 从 StartExamDto 到 ExamRecord 的映射
        CreateMap<StartExamDto, ExamRecord>()
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.Now))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ExamRecordStatus.InProgress))
            .ForMember(dest => dest.ScreenSwitchCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.CheatingSuspicionLevel, opt => opt.MapFrom(src => 0));
            
        // 从 ExamAnswerRecord 到 WrongQuestionDto 的映射
        CreateMap<ExamAnswerRecord, WrongQuestionDto>()
            .ForMember(dest => dest.ExamAnswerRecordId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ExamRecordId, opt => opt.MapFrom(src => src.ExamRecordId))
            .ForMember(dest => dest.ExamName, opt => opt.MapFrom(src => src.ExamRecord.ExamSetting.Name))
            .ForMember(dest => dest.QuestionContent, opt => opt.MapFrom(src => src.QuestionVersion.Content))
            .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.QuestionVersion.Question.Type.ToString()))
            .ForMember(dest => dest.QuestionScore, opt => opt.MapFrom(src => src.QuestionVersion.DefaultScore))
            .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.QuestionVersion.CorrectAnswer))
            .ForMember(dest => dest.Analysis, opt => opt.MapFrom(src => src.QuestionVersion.Analysis))
            .ForMember(dest => dest.ExamTime, opt => opt.MapFrom(src => src.ExamRecord.StartTime));
            
        // 添加 PageList 映射配置
        CreateMap<PageList<ExamRecord>, PageList<ExamRecordDto>>();
        CreateMap<PageList<ExamAnswerRecord>, PageList<WrongQuestionDto>>();
    }
} 