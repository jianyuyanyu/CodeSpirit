using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 练习设置服务接口
/// </summary>
public interface IPracticeSettingService : IBaseCRUDService<PracticeSetting, PracticeSettingDto, long, CreatePracticeSettingDto, UpdatePracticeSettingDto>
{
    /// <summary>
    /// 获取练习设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习设置分页列表</returns>
    Task<PageList<PracticeSettingDto>> GetPracticeSettingsAsync(PracticeSettingQueryDto queryDto);
    
    /// <summary>
    /// 发布练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    /// <returns>操作结果</returns>
    Task PublishPracticeSettingAsync(long id);
    
    /// <summary>
    /// 禁用练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    /// <returns>操作结果</returns>
    Task DisablePracticeSettingAsync(long id);
    
    /// <summary>
    /// 启用练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    /// <returns>操作结果</returns>
    Task EnablePracticeSettingAsync(long id);
    
    /// <summary>
    /// 获取试卷的所有练习设置
    /// </summary>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>练习设置列表</returns>
    Task<List<PracticeSettingDto>> GetPracticeSettingsByExamPaperIdAsync(long examPaperId);
} 