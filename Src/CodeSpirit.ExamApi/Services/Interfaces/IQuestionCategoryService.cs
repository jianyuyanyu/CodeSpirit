using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionCategory;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 题目分类服务接口
/// </summary>
public interface IQuestionCategoryService : IBaseCRUDService<QuestionCategory, QuestionCategoryDto, long, CreateQuestionCategoryDto, UpdateQuestionCategoryDto>
{    
    /// <summary>
    /// 获取题目分类分页列表
    /// </summary>
    Task<PageList<QuestionCategoryDto>> GetQuestionCategoriesAsync(QuestionCategoryQueryDto queryDto);
    
    /// <summary>
    /// 获取所有题目分类（用于树形选择）
    /// </summary>
    Task<List<QuestionCategoryDto>> GetAllCategoriesAsync();
} 