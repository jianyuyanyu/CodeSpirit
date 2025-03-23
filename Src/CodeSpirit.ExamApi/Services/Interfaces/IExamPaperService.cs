using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 试卷服务接口
/// </summary>
public interface IExamPaperService : IBaseCRUDService<ExamPaper, ExamPaperDto, long, CreateExamPaperDto, UpdateExamPaperDto>
{
    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    Task PublishExamPaperAsync(long id);
    
    /// <summary>
    /// 取消发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    Task UnpublishExamPaperAsync(long id);
    
    /// <summary>
    /// 根据规则随机生成试卷
    /// </summary>
    /// <param name="createDto">随机试卷创建DTO</param>
    /// <returns>生成的试卷</returns>
    Task<ExamPaperDto> GenerateRandomExamPaperAsync(GenerateRandomExamPaperDto createDto);
    
    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="id">源试卷ID</param>
    /// <returns>复制后的试卷</returns>
    Task<ExamPaperDto> CopyExamPaperAsync(long id);
    Task<PageList<ExamPaperDto>> GetExamPapersAsync(ExamPaperQueryDto queryDto);
    Task<IEnumerable<ExamPaperDto>> GetAllExamPapersByStatusAsync(ExamPaperStatus examPaperStatus = ExamPaperStatus.Published);
} 