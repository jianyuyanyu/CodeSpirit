using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Dtos.PublishHistory;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置项管理服务实现
/// </summary>
public class ConfigItemService : BaseCRUDIService<ConfigItem, ConfigItemDto, int, CreateConfigDto, UpdateConfigDto, ConfigItemBatchImportDto>, IConfigItemService
{
    private readonly IRepository<ConfigItem> repository;
    private readonly IRepository<App> _appRepository;
    private readonly IConfigCacheService _cacheService;
    private readonly IConfigNotificationService _notificationService;
    private readonly IConfigPublishHistoryService _publishHistoryService;
    private readonly ILogger<ConfigItemService> logger;

    /// <summary>
    /// 初始化配置项管理服务
    /// </summary>
    public ConfigItemService(
        IRepository<ConfigItem> repository,
        IRepository<App> appRepository,
        IConfigCacheService cacheService,
        IConfigNotificationService notificationService,
        IConfigPublishHistoryService publishHistoryService,
        IMapper mapper,
        ILogger<ConfigItemService> logger)
        : base(repository, mapper)
    {
        this.repository = repository;
        _appRepository = appRepository;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _publishHistoryService = publishHistoryService;
        this.logger = logger;
    }

    /// <summary>
    /// 获取指定配置项
    /// </summary>
    public async Task<ConfigItemDto> GetConfigAsync(string appId, string environment, string key)
    {
        ArgumentNullException.ThrowIfNull(appId);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // 尝试从缓存获取
            var cacheKey = GetConfigCacheKey(appId, environment, key);
            var cachedValue = await _cacheService.GetAsync(cacheKey);
            if (cachedValue != null)
            {
                return JsonConvert.DeserializeObject<ConfigItemDto>(cachedValue);
            }

            // 从数据库查询
            var predicate = BuildConfigItemPredicate(appId, environment, key);
            var config = await repository.Find(predicate).FirstOrDefaultAsync();

            if (config == null)
            {
                throw new AppServiceException(404, "配置不存在");
            }

            // 缓存配置
            await _cacheService.SetAsync(cacheKey, JsonConvert.SerializeObject(config));
            return Mapper.Map<ConfigItemDto>(config);
        }
        catch (Exception ex) when (ex is not AppServiceException)
        {
            logger.LogError(ex, "获取配置失败: {AppId}/{Environment}/{Key}", appId, environment, key);
            throw new AppServiceException(500, "获取配置失败");
        }
    }

    /// <summary>
    /// 获取配置项分页列表
    /// </summary>
    public async Task<PageList<ConfigItemDto>> GetConfigsAsync(ConfigItemQueryDto queryDto)
    {
        var predicate = BuildQueryPredicate(queryDto);
        return await GetPagedListAsync(queryDto, predicate, "App");
    }

    /// <summary>
    /// 获取应用在指定环境下的所有配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合</returns>
    public async Task<ConfigItemsExportDto> GetAppConfigsAsync(string appId, string environment)
    {
        // 构建查询条件
        var predicate = PredicateBuilder.New<ConfigItem>()
            .And(x => x.AppId == appId)
            .And(x => x.Environment.ToString() == environment)
            .And(x => x.Status == ConfigStatus.Released);

        // 获取配置列表，包含值类型
        var configItems = await repository.Find(predicate)
            .Select(x => new { x.Key, x.Value, x.ValueType })
            .ToListAsync();

        // 转换值为对应的类型
        var configs = new Dictionary<string, object>();
        foreach (var item in configItems)
        {
            configs[item.Key] = ConvertValueByType(item.Value, item.ValueType);
        }

        return new ConfigItemsExportDto
        {
            AppId = appId,
            Environment = environment,
            Configs = configs
        };
    }

    /// <summary>
    /// 获取应用在指定环境下的所有配置，包括从父级应用继承的配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合（包含继承的配置）</returns>
    public async Task<ConfigItemsExportDto> GetAppConfigsWithInheritanceAsync(string appId, string environment)
    {
        ArgumentNullException.ThrowIfNull(appId);
        ArgumentNullException.ThrowIfNull(environment);

        var app = await GetAppWithInheritanceInfo(appId);
        var configs = await GetAppConfigsDictionary(appId, environment);

        // 合并父级应用配置
        if (!string.IsNullOrEmpty(app.InheritancedAppId))
        {
            await MergeParentConfigs(configs, app.InheritancedAppId, environment);
        }

        return new ConfigItemsExportDto
        {
            AppId = appId,
            Environment = environment,
            Configs = configs,
            IncludesInheritedConfig = !string.IsNullOrEmpty(app.InheritancedAppId)
        };
    }

    /// <summary>
    /// 根据配置类型转换值
    /// </summary>
    private object ConvertValueByType(string value, ConfigValueType valueType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            return valueType switch
            {
                ConfigValueType.Boolean => bool.Parse(value),
                ConfigValueType.Int => int.Parse(value, CultureInfo.InvariantCulture),
                ConfigValueType.Double => double.Parse(value, CultureInfo.InvariantCulture),
                ConfigValueType.Json => JsonConvert.DeserializeObject(value),
                ConfigValueType.Encrypted => value, // 加密值保持为字符串
                ConfigValueType.String => value,
                _ => value
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "转换配置值失败，保持原始字符串: {Value}, 类型: {ValueType}", value, valueType);
            return value; // 转换失败时返回原始字符串
        }
    }

    /// <summary>
    /// 批量更新应用配置
    /// </summary>
    /// <param name="updateDto">更新请求数据</param>
    /// <returns>更新结果</returns>
    public async Task<(int successCount, List<string> failedKeys)> UpdateConfigCollectionAsync(ConfigItemsUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        try
        {
            var failedKeys = new List<string>();
            var successCount = 0;
            var unchangedCount = 0;

            // 获取现有配置
            var existingConfigs = await GetExistingReleasedConfigs(updateDto.AppId, updateDto.Environment);

            // 逐个处理配置项
            foreach (var property in updateDto.ParsedConfigs.Properties())
            {
                var key = property.Name;
                var value = property.Value.ToString();

                try
                {
                    // 检查配置是否存在且值未变更
                    if (existingConfigs.TryGetValue(key, out var existingConfig) &&
                        existingConfig.Value == value)
                    {
                        // 如果值未变更且已发布，则跳过更新
                        unchangedCount++;
                        continue;
                    }

                    await UpdateSingleConfig(
                        updateDto.AppId,
                        updateDto.Environment,
                        key,
                        value,
                        existingConfigs);

                    successCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "更新配置失败: {AppId}/{Environment}/{Key}",
                        updateDto.AppId, updateDto.Environment, key);
                    failedKeys.Add(key);
                }
            }

            if (successCount > 0)
            {
                await repository.SaveChangesAsync();
                logger.LogInformation("批量更新配置完成: {AppId}/{Environment}, 更新: {SuccessCount}, 跳过未变更: {UnchangedCount}, 失败: {FailedCount}",
                    updateDto.AppId, updateDto.Environment, successCount, unchangedCount, failedKeys.Count);
            }
            else
            {
                logger.LogInformation("批量更新配置完成: {AppId}/{Environment}, 所有配置项未变更，跳过: {UnchangedCount}",
                    updateDto.AppId, updateDto.Environment, unchangedCount);
            }

            return (successCount, failedKeys);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "批量更新配置失败: {AppId}/{Environment}",
                updateDto.AppId, updateDto.Environment);
            throw new AppServiceException(500, "批量更新配置失败");
        }
    }

    /// <summary>
    /// 批量发布配置项
    /// </summary>
    /// <param name="publishDto">批量发布请求数据</param>
    /// <returns>发布结果</returns>
    public async Task<(int successCount, List<int> failedIds)> BatchPublishAsync(ConfigItemsBatchPublishDto publishDto)
    {
        ArgumentNullException.ThrowIfNull(publishDto);

        if (publishDto.Ids == null || !publishDto.Ids.Any())
        {
            throw new AppServiceException(400, "请选择要发布的配置项");
        }

        // 获取待发布配置项
        var (configsToPublish, missingIds) = await FetchConfigsToPublish(publishDto.Ids);

        if (configsToPublish.Any(p => p.Status == ConfigStatus.Released))
        {
            throw new AppServiceException(200, "已发布的配置项无法再次发布！");
        }

        return await PublishConfigsWithTransaction(configsToPublish, missingIds, publishDto.Description);
    }

    /// <summary>
    /// 配置值类型判断结果
    /// </summary>
    private readonly record struct ConfigValueValidationResult(bool IsValid, ConfigValueType ValueType);

    /// <summary>
    /// 验证并推断配置值类型
    /// </summary>
    private ConfigValueValidationResult ValidateAndInferConfigValueType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new(true, ConfigValueType.String);
        }

        // 使用 AsSpan 避免不必要的字符串分配
        ReadOnlySpan<char> span = value.AsSpan().Trim();

        if (span.Length == 0)
        {
            return new(true, ConfigValueType.String);
        }

        // 快速路径：检查第一个字符来预判类型
        char firstChar = span[0];

        // 可能是数字或负数
        if (firstChar is '-' or '+' or >= '0' and <= '9')
        {
            if (TryParseNumber(span, out var type))
            {
                return new(true, type);
            }
        }
        // 可能是布尔值
        else if (firstChar is 't' or 'T' or 'f' or 'F')
        {
            if (IsBooleanValue(span))
            {
                return new(true, ConfigValueType.Boolean);
            }
        }
        // 可能是JSON
        else if (firstChar is '{' or '[')
        {
            if (IsJsonValue(value)) // 这里使用原始字符串，因为JSON解析需要string
            {
                return new(true, ConfigValueType.Json);
            }
        }

        return new(true, ConfigValueType.String);
    }

    /// <summary>
    /// 尝试解析数字类型
    /// </summary>
    private static bool TryParseNumber(ReadOnlySpan<char> span, out ConfigValueType type)
    {
        // 检查是否包含小数点或科学计数法标记
        bool containsDecimalPoint = span.Contains('.');
        bool containsExponent = span.Contains("e", StringComparison.OrdinalIgnoreCase) ||
                              span.Contains("E", StringComparison.OrdinalIgnoreCase);

        if (!containsDecimalPoint && !containsExponent)
        {
            // 尝试解析整数
            if (int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                type = ConfigValueType.Int;
                return true;
            }
        }

        // 尝试解析浮点数
        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            type = ConfigValueType.Double;
            return true;
        }

        type = ConfigValueType.String;
        return false;
    }

    /// <summary>
    /// 检查是否为布尔值
    /// </summary>
    private static bool IsBooleanValue(ReadOnlySpan<char> span) =>
        MemoryExtensions.Equals(span, "true".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
        MemoryExtensions.Equals(span, "false".AsSpan(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 检查是否为有效的JSON
    /// </summary>
    private static bool IsJsonValue(string value)
    {
        try
        {
            using var reader = new JsonTextReader(new StringReader(value));
            while (reader.Read())
            {
                // 只需要验证JSON的有效性，不需要实际解析
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证值是否符合指定的配置类型
    /// </summary>
    private bool ValidateValueForType(string value, ConfigValueType valueType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return valueType == ConfigValueType.String;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();

        return valueType switch
        {
            ConfigValueType.Boolean => IsBooleanValue(span),
            ConfigValueType.Int => TryParseNumber(span, out var type) && type == ConfigValueType.Int,
            ConfigValueType.Double => TryParseNumber(span, out _),
            ConfigValueType.Json => IsJsonValue(value),
            ConfigValueType.String => true,
            ConfigValueType.Encrypted => span.Length > 0,
            _ => false
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// 构建配置项查询条件
    /// </summary>
    private static ExpressionStarter<ConfigItem> BuildQueryPredicate(ConfigItemQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<ConfigItem>(true);

        if (!string.IsNullOrEmpty(queryDto.AppId))
        {
            predicate = predicate.And(x => x.AppId == queryDto.AppId);
        }
        if (queryDto.Environment.HasValue)
        {
            predicate = predicate.And(x => x.Environment == queryDto.Environment.Value);
        }
        if (!string.IsNullOrEmpty(queryDto.Group))
        {
            predicate = predicate.And(x => x.Group == queryDto.Group);
        }
        if (!string.IsNullOrEmpty(queryDto.Key))
        {
            predicate = predicate.And(x => x.Key.Contains(queryDto.Key));
        }
        if (queryDto.Status.HasValue)
        {
            predicate = predicate.And(x => x.Status == queryDto.Status.Value);
        }
        if (queryDto.ValueType.HasValue)
        {
            predicate = predicate.And(x => x.ValueType == queryDto.ValueType.Value);
        }

        return predicate;
    }

    /// <summary>
    /// 获取配置项缓存键
    /// </summary>
    private static string GetConfigCacheKey(string appId, string environment, string key) =>
        $"config:{appId}:{environment}:{key}";

    /// <summary>
    /// 构建配置项查询条件
    /// </summary>
    private static ExpressionStarter<ConfigItem> BuildConfigItemPredicate(string appId, string environment, string key)
    {
        return PredicateBuilder.New<ConfigItem>()
            .And(x => x.AppId == appId)
            .And(x => x.Environment.ToString() == environment)
            .And(x => x.Key == key);
    }

    /// <summary>
    /// 获取应用信息及继承关系
    /// </summary>
    private async Task<App> GetAppWithInheritanceInfo(string appId)
    {
        var app = await _appRepository.Find(x => x.Id == appId)
            .Include(x => x.InheritancedApp)
            .FirstOrDefaultAsync();

        if (app == null)
        {
            throw new AppServiceException(404, "应用不存在");
        }

        return app;
    }

    /// <summary>
    /// 获取应用配置并转换为字典
    /// </summary>
    private async Task<Dictionary<string, object>> GetAppConfigsDictionary(string appId, string environment)
    {
        var predicate = PredicateBuilder.New<ConfigItem>()
            .And(x => x.AppId == appId)
            .And(x => x.Environment.ToString() == environment)
            .And(x => x.Status == ConfigStatus.Released);

        var configItems = await repository.Find(predicate)
            .Select(x => new { x.Key, x.Value, x.ValueType })
            .ToListAsync();

        // 转换值为对应的类型
        var configs = new Dictionary<string, object>();
        foreach (var item in configItems)
        {
            configs[item.Key] = ConvertValueByType(item.Value, item.ValueType);
        }

        return configs;
    }

    /// <summary>
    /// 合并父级应用配置
    /// </summary>
    private async Task MergeParentConfigs(
        Dictionary<string, object> configs,
        string parentAppId,
        string environment)
    {
        try
        {
            // 递归获取父级应用配置
            var parentConfigs = await GetAppConfigsAsync(parentAppId, environment);
            if (parentConfigs?.Configs != null)
            {
                // 合并配置，子应用配置优先
                foreach (var kvp in parentConfigs.Configs)
                {
                    if (!configs.ContainsKey(kvp.Key))
                    {
                        configs[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 记录获取父级配置失败，但继续处理当前应用配置
            logger.LogWarning(ex, "获取父级应用 {ParentAppId} 配置失败，将仅使用当前应用配置", parentAppId);
        }
    }

    /// <summary>
    /// 获取现有配置
    /// </summary>
    private async Task<Dictionary<string, ConfigItem>> GetExistingReleasedConfigs(string appId, string environment)
    {
        return await repository.Find(x =>
                x.AppId == appId &&
                x.Status == ConfigStatus.Released &&
                x.Environment.ToString() == environment)
            .ToDictionaryAsync(x => x.Key);
    }

    /// <summary>
    /// 更新单个配置项
    /// </summary>
    private async Task UpdateSingleConfig(
        string appId,
        string environment,
        string key,
        string value,
        Dictionary<string, ConfigItem> existingConfigs)
    {
        // 验证并推断配置值类型
        var validationResult = ValidateAndInferConfigValueType(value);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("配置值格式无效: {AppId}/{Environment}/{Key}",
                appId, environment, key);
            throw new AppServiceException(400, "配置值格式无效");
        }

        if (existingConfigs.TryGetValue(key, out var existingConfig))
        {
            await UpdateExistingConfig(existingConfig, value, validationResult.ValueType, environment);
        }
        else
        {
            await CreateNewConfig(appId, environment, key, value, validationResult.ValueType);
        }

        // 清除缓存
        await _cacheService.RemoveAsync(GetConfigCacheKey(appId, environment, key));
    }

    /// <summary>
    /// 更新现有配置 - 如果已发布则创建新版本
    /// </summary>
    private async Task UpdateExistingConfig(
        ConfigItem existingConfig,
        string value,
        ConfigValueType inferredType,
        string environment)
    {
        // 检查配置项是否已发布
        // 对于已发布的配置项则创建新版本，处于编辑状态的配置项可以直接修改
        if (existingConfig.Environment.ToString() == environment)
        {
            // 创建新版本的配置项
            var newConfig = new ConfigItem
            {
                AppId = existingConfig.AppId,
                Key = existingConfig.Key,
                Value = value,
                Environment = existingConfig.Environment,
                ValueType = inferredType,
                Group = existingConfig.Group,
                Description = existingConfig.Description,
                Version = existingConfig.Version + 1,
                Status = ConfigStatus.Editing,
            };

            // 验证新配置项的值类型
            if (newConfig.ValueType != ConfigValueType.String && !ValidateValueForType(value, newConfig.ValueType))
            {
                throw new AppServiceException(400, $"配置值类型不匹配，期望类型: {newConfig.ValueType}");
            }

            // 添加新配置项
            await repository.AddAsync(newConfig);

            logger.LogInformation("已创建配置项新版本: {AppId}/{Environment}/{Key}, 原版本: {OldVersion}, 新版本: {NewVersion}",
                existingConfig.AppId, existingConfig.Environment, existingConfig.Key, existingConfig.Version, newConfig.Version);
        }
        else
        {
            // 配置项未发布，可以直接修改
            existingConfig.Value = value;
            existingConfig.Version++;
            existingConfig.Status = ConfigStatus.Editing;

            // 如果现有配置类型是String，则可以根据值推断更新类型
            if (existingConfig.ValueType == ConfigValueType.String)
            {
                existingConfig.ValueType = inferredType;
            }
            // 如果配置类型不匹配，则验证值是否符合现有类型
            else if (!ValidateValueForType(value, existingConfig.ValueType))
            {
                throw new AppServiceException(400, $"配置值类型不匹配，期望类型: {existingConfig.ValueType}");
            }

            await repository.UpdateAsync(existingConfig);
        }
    }

    /// <summary>
    /// 创建新配置
    /// </summary>
    private async Task CreateNewConfig(
        string appId,
        string environment,
        string key,
        string value,
        ConfigValueType valueType)
    {
        var newConfig = new ConfigItem
        {
            AppId = appId,
            Key = key,
            Value = value,
            Environment = Enum.Parse<EnvironmentType>(environment),
            ValueType = valueType,
            Version = 1,
            Status = ConfigStatus.Editing
        };

        await repository.AddAsync(newConfig);
    }

    /// <summary>
    /// 获取待发布的配置项
    /// </summary>
    private async Task<(List<ConfigItem> configs, List<int> missingIds)> FetchConfigsToPublish(IEnumerable<int> ids)
    {
        var distinctIds = ids.Distinct().ToList();
        var configsToPublish = await repository.Find(x => distinctIds.Contains(x.Id)).ToListAsync();

        // 找出缺失的配置ID
        var missingIds = new List<int>();
        if (configsToPublish.Count != distinctIds.Count)
        {
            var foundIds = configsToPublish.Select(c => c.Id).ToHashSet();
            missingIds = distinctIds.Where(id => !foundIds.Contains(id)).ToList();
            foreach (var id in missingIds)
            {
                logger.LogWarning("未找到配置项: ID={Id}", id);
            }
        }

        return (configsToPublish, missingIds);
    }

    /// <summary>
    /// 使用事务发布配置项
    /// </summary>
    private async Task<(int successCount, List<int> failedIds)> PublishConfigsWithTransaction(
        List<ConfigItem> configsToPublish,
        List<int> initialFailedIds,
        string description)
    {
        var failedIds = new List<int>(initialFailedIds);
        var successCount = 0;
        var successfullyPublishedConfigs = new List<(ConfigItem publishedConfig, string oldValue)>();

        try
        {
            // 使用事务确保操作的原子性
            await repository.ExecuteInTransactionAsync(async () =>
            {
                // 批量更新配置项状态
                foreach (var config in configsToPublish)
                {
                    try
                    {
                        // 查找同一应用、环境、键的已发布配置项
                        var existingPublishedConfig = await repository.Find(x =>
                            x.AppId == config.AppId &&
                            x.Environment == config.Environment &&
                            x.Key == config.Key &&
                            x.Status == ConfigStatus.Released)
                            .FirstOrDefaultAsync();

                        string oldValue = null;

                        if (existingPublishedConfig != null)
                        {
                            // 记录旧值用于历史记录
                            oldValue = existingPublishedConfig.Value;

                            if (existingPublishedConfig.Id != config.Id)
                            {
                                // 更新已发布配置的值并保留其ID
                                existingPublishedConfig.Value = config.Value;
                                existingPublishedConfig.ValueType = config.ValueType;
                                existingPublishedConfig.Description = config.Description;
                                existingPublishedConfig.Group = config.Group;
                                existingPublishedConfig.Version = config.Version;
                                await repository.UpdateAsync(existingPublishedConfig);

                                // 删除当前配置项(如果不是同一个ID)
                                await repository.DeleteAsync(config);

                                // 记录成功发布的配置(使用已发布的配置ID)
                                successfullyPublishedConfigs.Add((existingPublishedConfig, oldValue));
                                logger.LogInformation("更新已发布配置: {AppId}/{Environment}/{Key}, ID从{OldId}到{NewId}",
                                    config.AppId, config.Environment, config.Key, config.Id, existingPublishedConfig.Id);
                            }
                            else
                            {
                                // 如果是同一个ID，简单更新即可
                                successfullyPublishedConfigs.Add((config, oldValue));
                            }
                        }
                        else
                        {
                            // 没有已发布配置，直接发布当前配置
                            config.Status = ConfigStatus.Released;
                            await repository.UpdateAsync(config);
                            successfullyPublishedConfigs.Add((config, null));
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "发布配置失败: ID={Id}", config.Id);
                        failedIds.Add(config.Id);
                    }
                }

                if (successfullyPublishedConfigs.Any())
                {
                    // 创建发布历史记录
                    await CreatePublishHistoryWithOriginalValues(successfullyPublishedConfigs, description);
                }
            });

            // 事务完成后，清除缓存
            await ClearCacheForPublishedConfigs(successfullyPublishedConfigs.Select(x => x.publishedConfig).ToList());

            return (successCount, failedIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "批量发布配置失败");
            throw new AppServiceException(500, "批量发布配置失败");
        }
    }

    /// <summary>
    /// 创建发布历史记录（使用原始值）
    /// </summary>
    private async Task CreatePublishHistoryWithOriginalValues(List<(ConfigItem publishedConfig, string oldValue)> publishedConfigsWithValues, string description)
    {
        try
        {
            var firstConfig = publishedConfigsWithValues.First().publishedConfig;
            string appId = firstConfig.AppId;
            string environment = firstConfig.Environment.ToString();

            // 创建发布历史DTO
            var createHistoryDto = new CreateConfigPublishHistoryDto
            {
                AppId = appId,
                Environment = environment,
                Description = description ?? "批量发布配置",
                ConfigItems = publishedConfigsWithValues.Select(item =>
                {
                    var (config, oldValue) = item;
                    return new ConfigItemForPublishDto
                    {
                        Id = config.Id,
                        Value = config.Value,
                        Version = config.Version,
                        OldValue = oldValue // 使用直接获取的原始值
                    };
                }).ToList()
            };

            // 使用DTO创建历史记录
            await _publishHistoryService.CreatePublishHistoryAsync(createHistoryDto);

            logger.LogInformation("创建发布历史记录成功: {AppId}/{Environment}, 共{Count}个配置项",
                appId, environment, publishedConfigsWithValues.Count);
        }
        catch (Exception ex)
        {
            // 记录日志但不影响发布结果
            logger.LogError(ex, "创建发布历史记录失败");
        }
    }

    /// <summary>
    /// 清除已发布配置的缓存
    /// </summary>
    private async Task ClearCacheForPublishedConfigs(List<ConfigItem> publishedConfigs)
    {
        foreach (var config in publishedConfigs)
        {
            var cacheKey = GetConfigCacheKey(config.AppId, config.Environment.ToString(), config.Key);
            await _cacheService.RemoveAsync(cacheKey);
        }
    }

    #endregion

    #region Override Base Methods

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateConfigDto createDto)
    {
        // 验证应用是否存在
        App app = await _appRepository.GetByIdAsync(createDto.AppId);
        if (app == null)
        {
            throw new AppServiceException(404, "应用不存在");
        }

        // 验证配置是否已存在
        ConfigItem exists = await Repository.Find(x =>
            x.AppId == createDto.AppId &&
            x.Environment == createDto.Environment &&
            x.Key == createDto.Key).FirstOrDefaultAsync();

        if (exists != null)
        {
            throw new AppServiceException(400, "配置已存在");
        }
    }

    /// <summary>
    /// 创建实体后的处理
    /// </summary>
    protected override async Task OnCreated(ConfigItem entity, CreateConfigDto createConfigDto)
    {
        // 清除缓存
        await _cacheService.RemoveAsync($"config:{entity.AppId}:{entity.Environment}:{entity.Key}");

        // 发送配置变更通知
        await _notificationService.NotifyConfigChangedAsync(
            entity.AppId, entity.Environment.ToString());
    }

    protected override Task OnUpdating(ConfigItem entity, UpdateConfigDto updateDto)
    {
        entity.Status = ConfigStatus.Editing;
        return base.OnUpdating(entity, updateDto);
    }
    /// <summary>
    /// 更新实体后的处理
    /// </summary>
    protected override async Task OnUpdated(ConfigItem entity)
    {
        // 清除缓存
        await _cacheService.RemoveAsync($"config:{entity.AppId}:{entity.Environment}:{entity.Key}");

        // 发送配置变更通知
        await _notificationService.NotifyConfigChangedAsync(
            entity.AppId, entity.Environment.ToString());
    }

    /// <summary>
    /// 删除实体后的处理
    /// </summary>
    protected override async Task OnDeleted(ConfigItem entity)
    {
        // 清除缓存
        await _cacheService.RemoveAsync($"config:{entity.AppId}:{entity.Environment}:{entity.Key}");

        // 发送配置变更通知
        await _notificationService.NotifyConfigChangedAsync(
            entity.AppId, entity.Environment.ToString());
    }

    protected override string GetImportItemId(ConfigItemBatchImportDto importDto)
    {
        return importDto.AppId;
    }
    #endregion
}