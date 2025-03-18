using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 题目版本映射配置
/// </summary>
public class QuestionVersionProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionVersionProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            QuestionVersion, 
            QuestionVersionDto, 
            long, 
            CreateQuestionVersionDto, 
            UpdateQuestionVersionDto,
            CreateQuestionVersionDto>();

        // 自定义映射
        CreateMap<QuestionVersion, QuestionVersionDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
    }
} 