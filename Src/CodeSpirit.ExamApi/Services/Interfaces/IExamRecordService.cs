using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试记录服务接口
/// </summary>
public interface IExamRecordService : IBaseCRUDService<ExamRecord, ExamRecordDto, long, StartExamDto, object>
{
    /// <summary>
    /// 开始考试
    /// </summary>
    /// <param name="startExamDto">开始考试参数</param>
    /// <returns>考试记录DTO</returns>
    Task<ExamRecordDto> StartExamAsync(StartExamDto startExamDto);
    
    /// <summary>
    /// 提交答案
    /// </summary>
    /// <param name="submitAnswerDto">提交答案参数</param>
    /// <returns>是否成功</returns>
    Task<bool> SubmitAnswerAsync(SubmitAnswerDto submitAnswerDto);
    
    /// <summary>
    /// 完成考试
    /// </summary>
    /// <param name="finishExamDto">完成考试参数</param>
    /// <returns>考试结果</returns>
    Task<ExamRecordDto> FinishExamAsync(FinishExamDto finishExamDto);
    
    /// <summary>
    /// 获取考试统计
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <returns>考试统计DTO</returns>
    Task<ExamStatisticsDto> GetExamStatisticsAsync(long examSettingId);
    
    /// <summary>
    /// 获取错题列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>错题列表</returns>
    Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto);
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RecordScreenSwitchAsync(long recordId);
    
    /// <summary>
    /// 获取考试记录及答题详情
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录详情</returns>
    Task<ExamRecordDto> GetExamRecordDetailAsync(long recordId);
    /// <summary>
    /// 获取答题预览要素
    /// </summary>
    /// <param name="recordId"></param>
    /// <returns></returns>
    Task<AnswerPreviewDto> GetAnswerPreviewAsync(long recordId);
} 