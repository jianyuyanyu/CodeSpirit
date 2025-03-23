using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试设置服务接口
/// </summary>
public interface IExamSettingService : IBaseCRUDService<ExamSetting, ExamSettingDto, long, CreateExamSettingDto, UpdateExamSettingDto>
{
    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考试设置分页列表</returns>
    Task<PageList<ExamSettingDto>> GetExamSettingsAsync(ExamSettingQueryDto queryDto);
    
    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>考试设置详情</returns>
    Task<ExamSettingDto> GetExamSettingDetailAsync(long id);
    
    /// <summary>
    /// 发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>操作结果</returns>
    Task PublishExamSettingAsync(long id);
    
    /// <summary>
    /// 取消发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>操作结果</returns>
    Task UnpublishExamSettingAsync(long id);
} 