using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置发布历史服务接口
/// </summary>
public interface IConfigPublishHistoryService : 
    IBaseCRUDService<ConfigPublishHistory, ConfigPublishHistoryDto, int, CreateConfigPublishHistoryDto, UpdateConfigPublishHistoryDto>,
    IScopedDependency
{
    /// <summary>
    /// 获取应用的发布历史记录
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>发布历史分页列表</returns>
    Task<PageList<ConfigPublishHistoryDto>> GetPublishHistoryListAsync(ConfigPublishHistoryQueryDto queryDto);

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    /// <param name="publishHistoryId">发布历史ID</param>
    /// <returns>发布历史详情</returns>
    Task<ConfigPublishHistoryDto> GetPublishHistoryDetailAsync(int publishHistoryId);

    /// <summary>
    /// 创建发布历史记录
    /// </summary>
    /// <param name="createDto">创建参数</param>
    /// <returns>创建的发布历史记录</returns>
    Task<ConfigPublishHistoryDto> CreatePublishHistoryAsync(CreateConfigPublishHistoryDto createDto);
    
    /// <summary>
    /// 创建发布历史记录 (兼容旧接口)
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="description">发布描述</param>
    /// <param name="configItems">配置项列表</param>
    /// <returns>创建的发布历史记录</returns>
    Task<ConfigPublishHistory> CreatePublishHistoryAsync(
        string appId, string environment, string description, IEnumerable<ConfigItem> configItems);
    
    /// <summary>
    /// 回滚到指定的发布历史
    /// </summary>
    /// <param name="publishHistoryId">发布历史ID</param>
    /// <returns>回滚结果</returns>
    Task<(bool success, string message)> RollbackToHistoryAsync(int publishHistoryId);
} 