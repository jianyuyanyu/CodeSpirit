using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置发布历史服务接口
/// </summary>
public interface IConfigPublishHistoryService : IScopedDependency
{
    /// <summary>
    /// 获取应用的发布历史记录
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">分页大小</param>
    /// <returns>发布历史分页列表</returns>
    Task<PageList<ConfigPublishHistory>> GetPublishHistoryListAsync(string appId, string environment, int pageIndex = 1, int pageSize = 20);

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    /// <param name="publishHistoryId">发布历史ID</param>
    /// <returns>发布历史详情，包含所有发布的配置项</returns>
    Task<ConfigPublishHistory> GetPublishHistoryDetailAsync(int publishHistoryId);

    /// <summary>
    /// 创建发布历史记录
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="description">发布说明</param>
    /// <param name="configItems">配置项集合</param>
    /// <returns>创建的发布历史记录</returns>
    Task<ConfigPublishHistory> CreatePublishHistoryAsync(string appId, string environment, string description, IEnumerable<ConfigItem> configItems);

    /// <summary>
    /// 回滚到指定的发布历史
    /// </summary>
    /// <param name="publishHistoryId">发布历史ID</param>
    /// <returns>回滚操作结果</returns>
    Task<(bool success, string message)> RollbackToHistoryAsync(int publishHistoryId);
} 