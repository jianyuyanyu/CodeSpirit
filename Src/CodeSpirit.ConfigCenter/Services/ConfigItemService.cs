using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置项管理服务实现
/// </summary>
public class ConfigItemService : BaseService<ConfigItem, ConfigItemDto, int, CreateConfigDto, UpdateConfigDto, ConfigItemBatchImportDto>, IConfigItemService
{
    private readonly IRepository<ConfigItem> repository;
    private readonly IRepository<App> _appRepository;
    private readonly IConfigCacheService _cacheService;
    private readonly ILogger<ConfigItemService> logger;

    /// <summary>
    /// 初始化配置项管理服务
    /// </summary>
    public ConfigItemService(
        IRepository<ConfigItem> repository,
        IRepository<App> appRepository,
        IConfigCacheService cacheService,
        IMapper mapper,
        ILogger<ConfigItemService> logger)
        : base(repository, mapper)
    {
        this.repository = repository;
        _appRepository = appRepository;
        _cacheService = cacheService;
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
        if (queryDto.OnlineStatus.HasValue)
        {
            predicate = predicate.And(x => x.OnlineStatus == queryDto.OnlineStatus.Value);
        }
        if (queryDto.IsEnabled.HasValue)
        {
            predicate = predicate.And(x => x.IsEnabled == queryDto.IsEnabled.Value);
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
                .And(x => x.IsEnabled)
                .And(x => x.Status == ConfigStatus.Released);

            // 获取配置列表
            var configs = await repository.Find(predicate)
                .Select(x => new { x.Key, x.Value })
                .ToDictionaryAsync(x => x.Key, x => x.Value);

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

            foreach (var (key, value) in updateDto.Configs)
            {
                try
                {
                    // 验证并推断配置值类型
                    var (isValid, valueType) = ValidateAndInferConfigValueType(value);
                    if (!isValid)
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
                        existingConfig.OnlineStatus = false;
                        // 如果现有配置类型是String，则可以根据值推断更新类型
                        if (existingConfig.ValueType == ConfigValueType.String)
                        {
                            existingConfig.ValueType = valueType;
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
                            ValueType = valueType,
                            Version = 1,
                            Status = ConfigStatus.Editing,
                            IsEnabled = true
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
    /// 验证并推断配置值类型
    /// </summary>
    private (bool isValid, ConfigValueType valueType) ValidateAndInferConfigValueType(string value)
    {
        // 空值默认为字符串类型
        if (string.IsNullOrEmpty(value))
        {
            return (true, ConfigValueType.String);
        }

        // 尝试解析为布尔值
        if (bool.TryParse(value, out _))
        {
            return (true, ConfigValueType.Boolean);
        }

        // 尝试解析为整数
        if (int.TryParse(value, out _))
        {
            return (true, ConfigValueType.Int);
        }

        // 尝试解析为浮点数
        if (double.TryParse(value, out _))
        {
            return (true, ConfigValueType.Double);
        }

        // 尝试解析为JSON
        try
        {
            JsonConvert.DeserializeObject(value);
            return (true, ConfigValueType.Json);
        }
        catch
        {
            // 不是有效的JSON，视为普通字符串
        }

        // 默认为字符串类型
        return (true, ConfigValueType.String);
    }

    /// <summary>
    /// 验证值是否符合指定的配置类型
    /// </summary>
    private bool ValidateValueForType(string value, ConfigValueType valueType)
    {
        try
        {
            switch (valueType)
            {
                case ConfigValueType.Boolean:
                    return bool.TryParse(value, out _);
                case ConfigValueType.Int:
                    return int.TryParse(value, out _);
                case ConfigValueType.Double:
                    return double.TryParse(value, out _);
                case ConfigValueType.Json:
                    JsonConvert.DeserializeObject(value);
                    return true;
                case ConfigValueType.String:
                    return true;
                case ConfigValueType.Encrypted:
                    // 加密类型的值应该已经是加密后的格式
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
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
        //if (app.AutoPublish)
        //{
        //    await PublishConfigAsync(config.AppId, config.Environment, "自动发布新配置", "system");
        //}
    }

    /// <summary>
    /// 更新实体后的处理
    /// </summary>
    protected override async Task OnUpdated(ConfigItem entity)
    {
        // 清除缓存
        await _cacheService.RemoveAsync($"config:{entity.AppId}:{entity.Environment}:{entity.Key}");
    }

    /// <summary>
    /// 删除实体后的处理
    /// </summary>
    protected override async Task OnDeleted(ConfigItem entity)
    {
        // 清除缓存
        await _cacheService.RemoveAsync($"config:{entity.AppId}:{entity.Environment}:{entity.Key}");
    }

    protected override string GetImportItemId(ConfigItemBatchImportDto importDto)
    {
        return importDto.AppId;
    }
    #endregion
}