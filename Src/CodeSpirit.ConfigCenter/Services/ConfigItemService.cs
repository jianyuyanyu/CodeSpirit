using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
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
           predicate
       );
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

    protected override Task<ConfigItem> GetEntityForUpdate(int id,UpdateConfigDto updateDto)
    {
        throw new NotImplementedException();
    }

    protected override string GetImportItemId(ConfigItemBatchImportDto importDto)
    {
        throw new NotImplementedException();
    }

    #endregion
}