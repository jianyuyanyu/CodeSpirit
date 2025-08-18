using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionCategory;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 题目分类映射配置
/// </summary>
public class QuestionCategoryProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionCategoryProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            QuestionCategory, 
            QuestionCategoryDto, 
            long, 
            CreateQuestionCategoryDto, 
            UpdateQuestionCategoryDto,
            CreateQuestionCategoryDto>();
            
        // 自定义映射
        CreateMap<QuestionCategory, QuestionCategoryDto>()
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count));
            
        // 树形结构映射
        CreateMap<QuestionCategory, QuestionCategoryTreeDto>();
    }
} 