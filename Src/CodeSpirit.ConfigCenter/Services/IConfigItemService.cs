using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置项管理服务接口
/// </summary>
public interface IConfigItemService : IBaseService<ConfigItem, ConfigItemDto, int, CreateConfigDto, UpdateConfigDto, ConfigItemBatchImportDto>, IScopedDependency
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
} 