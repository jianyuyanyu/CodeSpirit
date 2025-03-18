using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.WrongQuestion;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 错题服务接口
/// </summary>
public interface IWrongQuestionService : IBaseCRUDService<WrongQuestion, WrongQuestionDto, long, CreateWrongQuestionDto, UpdateWrongQuestionDto>
{    
    /// <summary>
    /// 获取错题分页列表
    /// </summary>
    Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto);
    
    /// <summary>
    /// 获取考生的错题列表
    /// </summary>
    Task<List<WrongQuestionDto>> GetStudentWrongQuestionsAsync(long studentId);
    
    /// <summary>
    /// 记录错题（如果已存在则更新错误次数）
    /// </summary>
    Task<WrongQuestionDto> RecordWrongQuestionAsync(CreateWrongQuestionDto createDto);
} 