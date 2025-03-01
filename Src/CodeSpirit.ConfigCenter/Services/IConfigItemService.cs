using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置项管理服务接口
/// </summary>
public interface IConfigItemService : IBaseCRUDIService<ConfigItem, ConfigItemDto, int, CreateConfigDto, UpdateConfigDto, ConfigItemBatchImportDto>, IScopedDependency
{
    /// <summary>
    /// 获取指定配置项
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="key">配置键</param>
    /// <returns>配置项DTO</returns>
    Task<ConfigItemDto> GetConfigAsync(string appId, string environment, string key);

    /// <summary>
    /// 获取配置项分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页列表</returns>
    Task<PageList<ConfigItemDto>> GetConfigsAsync(ConfigItemQueryDto queryDto);

    /// <summary>
    /// 获取应用在指定环境下的所有配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合</returns>
    Task<ConfigItemsExportDto> GetAppConfigsAsync(string appId, string environment);

    /// <summary>
    /// 批量更新应用配置
    /// </summary>
    /// <param name="updateDto">更新请求数据</param>
    /// <returns>更新结果</returns>
    Task<(int successCount, List<string> failedKeys)> UpdateConfigCollectionAsync(ConfigItemsUpdateDto updateDto);

    /// <summary>
    /// 批量发布配置项
    /// </summary>
    /// <param name="publishDto">批量发布请求数据</param>
    /// <returns>发布结果，包含成功数量和失败的配置项ID列表</returns>
    Task<(int successCount, List<int> failedIds)> BatchPublishAsync(ConfigItemsBatchPublishDto publishDto);

    /// <summary>
    /// 获取应用在指定环境下的所有配置，包括从父级应用继承的配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合（包含继承的配置）</returns>
    Task<ConfigItemsExportDto> GetAppConfigsWithInheritanceAsync(string appId, string environment);
}