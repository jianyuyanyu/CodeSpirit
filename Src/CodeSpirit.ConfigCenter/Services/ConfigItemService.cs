using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
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
        try
        {
            // 尝试从缓存获取
            string cacheKey = $"config:{appId}:{environment}:{key}";
            string cachedValue = await _cacheService.GetAsync(cacheKey);
            if (cachedValue != null)
            {
                return JsonConvert.DeserializeObject<ConfigItemDto>(cachedValue);
            }

            System.Linq.Expressions.Expression<Func<ConfigItem, bool>> predicate = PredicateBuilder.New<ConfigItem>()
                .And(x => x.AppId == appId)
                .And(x => x.Environment.ToString() == environment)
                .And(x => x.Key == key);

            ConfigItem config = await repository.Find(predicate).FirstOrDefaultAsync();
            if (config == null)
            {
                throw new AppServiceException(404, "配置不存在");
            }

            // 缓存配置
            await _cacheService.SetAsync(cacheKey, JsonConvert.SerializeObject(config));
            return config != null ? Mapper.Map<ConfigItemDto>(config) : null;
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
        ExpressionStarter<ConfigItem> predicate = PredicateBuilder.New<ConfigItem>(true);

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

        return await GetPagedListAsync(
           queryDto,
           predicate,
           "App"
       );
    }

    /// <summary>
    /// 获取应用在指定环境下的所有配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合</returns>
    public async Task<ConfigItemsExportDto> GetAppConfigsAsync(string appId, string environment)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "获取应用配置失败: {AppId}/{Environment}", appId, environment);
            throw new AppServiceException(500, "获取应用配置失败");
        }
    }

    /// <summary>
    /// 获取应用在指定环境下的所有配置，包括从父级应用继承的配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>配置集合（包含继承的配置）</returns>
    public async Task<ConfigItemsExportDto> GetAppConfigsWithInheritanceAsync(string appId, string environment)
    {
        try
        {
            // 获取应用信息，包括继承关系
            var app = await _appRepository.Find(x => x.Id == appId)
                .Include(x => x.InheritancedApp)
                .FirstOrDefaultAsync();

            if (app == null)
            {
                throw new AppServiceException(404, "应用不存在");
            }

            // 获取应用自身的配置
            var predicate = PredicateBuilder.New<ConfigItem>()
                .And(x => x.AppId == appId)
                .And(x => x.Environment.ToString() == environment)
                .And(x => x.Status == ConfigStatus.Released);

            var configItems = await repository.Find(predicate)
                .Select(x => new { x.Key, x.Value, x.ValueType })
                .ToListAsync();

            // 转换值为对应的类型并存储到字典中
            var configs = new Dictionary<string, object>();
            foreach (var item in configItems)
            {
                configs[item.Key] = ConvertValueByType(item.Value, item.ValueType);
            }

            // 如果应用有父级应用，则获取父级应用的配置并合并
            if (!string.IsNullOrEmpty(app.InheritancedAppId))
            {
                try
                {
                    // 递归获取父级应用配置
                    var parentConfigs = await GetAppConfigsAsync(app.InheritancedAppId, environment);
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
                    logger.LogWarning(ex, "获取父级应用 {ParentAppId} 配置失败，将仅使用当前应用配置", app.InheritancedAppId);
                }
            }

            return new ConfigItemsExportDto
            {
                AppId = appId,
                Environment = environment,
                Configs = configs,
                IncludesInheritedConfig = !string.IsNullOrEmpty(app.InheritancedAppId)
            };
        }
        catch (Exception ex) when (ex is not AppServiceException)
        {
            logger.LogError(ex, "获取应用配置（包含继承）失败: {AppId}/{Environment}", appId, environment);
            throw new AppServiceException(500, "获取应用配置失败");
        }
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
        try
        {
            var failedKeys = new List<string>();
            var successCount = 0;

            // 获取现有配置
            var existingConfigs = await repository.Find(x =>
                x.AppId == updateDto.AppId &&
                x.Environment.ToString() == updateDto.Environment)
                .ToDictionaryAsync(x => x.Key);

            // 使用解析后的配置对象
            foreach (var property in updateDto.ParsedConfigs.Properties())
            {
                var key = property.Name;
                var value = property.Value.ToString();

                try
                {
                    // 验证并推断配置值类型
                    var validationResult = ValidateAndInferConfigValueType(value);
                    if (!validationResult.IsValid)
                    {
                        logger.LogWarning("配置值格式无效: {AppId}/{Environment}/{Key}",
                            updateDto.AppId, updateDto.Environment, key);
                        failedKeys.Add(key);
                        continue;
                    }

                    if (existingConfigs.TryGetValue(key, out var existingConfig))
                    {
                        // 更新现有配置
                        existingConfig.Value = value;
                        existingConfig.Version++;
                        existingConfig.Status = ConfigStatus.Editing;
                        // 如果现有配置类型是String，则可以根据值推断更新类型
                        if (existingConfig.ValueType == ConfigValueType.String)
                        {
                            existingConfig.ValueType = validationResult.ValueType;
                        }
                        // 如果配置类型不匹配，则验证值是否符合现有类型
                        else if (!ValidateValueForType(value, existingConfig.ValueType))
                        {
                            throw new AppServiceException(400, $"配置值类型不匹配，期望类型: {existingConfig.ValueType}");
                        }

                        await repository.UpdateAsync(existingConfig);
                    }
                    else
                    {
                        // 创建新配置
                        var newConfig = new ConfigItem
                        {
                            AppId = updateDto.AppId,
                            Key = key,
                            Value = value,
                            Environment = Enum.Parse<EnvironmentType>(updateDto.Environment),
                            ValueType = validationResult.ValueType,
                            Version = 1,
                            Status = ConfigStatus.Editing
                        };

                        await repository.AddAsync(newConfig);
                    }

                    // 清除缓存
                    string cacheKey = $"config:{updateDto.AppId}:{updateDto.Environment}:{key}";
                    await _cacheService.RemoveAsync(cacheKey);

                    successCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "更新配置失败: {AppId}/{Environment}/{Key}",
                        updateDto.AppId, updateDto.Environment, key);
                    failedKeys.Add(key);
                }
            }

            await repository.SaveChangesAsync();
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
        if (publishDto.Ids == null || !publishDto.Ids.Any())
        {
            throw new AppServiceException(400, "请选择要发布的配置项");
        }

        var failedIds = new List<int>();
        var successCount = 0;
        var successfullyPublishedConfigs = new List<ConfigItem>();

        // 获取所有匹配的配置项 - 使用一次查询获取所有数据
        var distinctIds = publishDto.Ids.Distinct().ToList();
        var configsToPublish = await repository.Find(x => distinctIds.Contains(x.Id)).ToListAsync();

        // 找出缺失的配置ID
        if (configsToPublish.Count != distinctIds.Count)
        {
            var foundIds = configsToPublish.Select(c => c.Id).ToHashSet();
            var missingIds = distinctIds.Where(id => !foundIds.Contains(id)).ToList();
            foreach (var id in missingIds)
            {
                failedIds.Add(id);
                logger.LogWarning("未找到配置项: ID={Id}", id);
            }
        }

        if (configsToPublish.Any(p => p.Status == ConfigStatus.Released))
        {
            throw new AppServiceException(200, "已发布的配置项无法再次发布！");
        }

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
                        config.Status = ConfigStatus.Released;
                        await repository.UpdateAsync(config);
                        successCount++;
                        successfullyPublishedConfigs.Add(config);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "发布配置失败: ID={Id}", config.Id);
                        failedIds.Add(config.Id);
                        // 不抛出异常，继续处理其他配置项
                    }
                }

                // 如果有成功发布的配置项，则创建发布历史记录
                if (successfullyPublishedConfigs.Any())
                {
                    try
                    {
                        // 获取发布的应用和环境信息（假设所有配置项属于同一应用和环境）
                        var firstConfig = successfullyPublishedConfigs.First();
                        string appId = firstConfig.AppId;
                        string environment = firstConfig.Environment.ToString();

                        // 创建发布历史记录（在同一事务中）
                        await _publishHistoryService.CreatePublishHistoryAsync(
                            appId,
                            environment,
                            publishDto.Description ?? "批量发布配置",
                            successfullyPublishedConfigs);

                        logger.LogInformation("创建发布历史记录成功: {AppId}/{Environment}, 共{Count}个配置项",
                            appId, environment, successfullyPublishedConfigs.Count);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志但不影响发布结果
                        logger.LogError(ex, "创建发布历史记录失败");
                    }
                }

                // 事务结束时自动保存更改，无需显式调用
            });

            // 事务完成后，清除缓存（缓存操作不应在事务中执行）
            foreach (var config in successfullyPublishedConfigs)
            {
                string cacheKey = $"config:{config.AppId}:{config.Environment}:{config.Key}";
                await _cacheService.RemoveAsync(cacheKey);
            }

            return (successCount, failedIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "批量发布配置失败");
            throw new AppServiceException(500, "批量发布配置失败");
        }
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