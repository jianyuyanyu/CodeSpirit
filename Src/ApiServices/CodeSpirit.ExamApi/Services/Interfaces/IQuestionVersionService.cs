using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 题目版本服务接口
/// </summary>
public interface IQuestionVersionService : IBaseCRUDService<QuestionVersion, QuestionVersionDto, long, CreateQuestionVersionDto, UpdateQuestionVersionDto>
{
    /// <summary>
    /// 获取题目版本分页列表
    /// </summary>
    Task<PageList<QuestionVersionDto>> GetQuestionVersionsAsync(QuestionVersionQueryDto queryDto);
    
    /// <summary>
    /// 获取题目的所有版本
    /// </summary>
    Task<List<QuestionVersionDto>> GetVersionsByQuestionIdAsync(long questionId);
    
    /// <summary>
    /// 获取题目的特定版本
    /// </summary>
    Task<QuestionVersionDto> GetQuestionVersionAsync(long questionId, int version);
    
    /// <summary>
    /// 创建新版本
    /// </summary>
    Task<QuestionVersionDto> CreateNewVersionAsync(CreateQuestionVersionDto createDto);
} 