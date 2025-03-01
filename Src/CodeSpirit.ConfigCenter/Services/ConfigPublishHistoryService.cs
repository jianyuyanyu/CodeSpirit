using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置发布历史服务实现
/// </summary>
public class ConfigPublishHistoryService : BaseCRUDService<ConfigPublishHistory, ConfigPublishHistoryDto, int, CreateConfigPublishHistoryDto, UpdateConfigPublishHistoryDto>, IConfigPublishHistoryService
{
    private readonly IRepository<ConfigPublishHistory> _publishHistoryRepository;
    private readonly IRepository<ConfigItemPublishHistory> _configItemHistoryRepository;
    private readonly IRepository<ConfigItem> _configItemRepository;
    private readonly IConfigCacheService _cacheService;
    private readonly IConfigNotificationService _notificationService;
    private readonly ILogger<ConfigPublishHistoryService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigPublishHistoryService(
        IRepository<ConfigPublishHistory> publishHistoryRepository,
        IRepository<ConfigItemPublishHistory> configItemHistoryRepository,
        IRepository<ConfigItem> configItemRepository,
        IConfigCacheService cacheService,
        IConfigNotificationService notificationService,
        IMapper mapper,
        ILogger<ConfigPublishHistoryService> logger)
        : base(publishHistoryRepository, mapper)
    {
        _publishHistoryRepository = publishHistoryRepository;
        _configItemHistoryRepository = configItemHistoryRepository;
        _configItemRepository = configItemRepository;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用的发布历史记录
    /// </summary>
    public async Task<PageList<ConfigPublishHistoryDto>> GetPublishHistoryListAsync(ConfigPublishHistoryQueryDto queryDto)
    {
        // 构建查询条件
        var predicate = PredicateBuilder.New<ConfigPublishHistory>(true);
        if (!string.IsNullOrEmpty(queryDto.AppId))
        {
            predicate = predicate.And(x => x.AppId == queryDto.AppId);
        }

        if (queryDto.Environment.HasValue)
        {
            predicate = predicate.And(x => x.Environment == queryDto.Environment.Value.ToString());
        }

        // 使用基类的分页方法
        return await GetPagedListAsync(
            queryDto,
            predicate,
            ["App", "ConfigItemPublishHistories"]
        );
    }

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    public async Task<ConfigPublishHistoryDto> GetPublishHistoryDetailAsync(int publishHistoryId)
    {
        // 获取发布历史，包含所有配置项变更记录
        var publishHistory = await _publishHistoryRepository.Find(h => h.Id == publishHistoryId)
            .Include(h => h.ConfigItemPublishHistories)
            .ThenInclude(h => h.ConfigItem)
            .FirstOrDefaultAsync();

        if (publishHistory == null)
        {
            throw new AppServiceException(404, "发布历史记录不存在");
        }

        return Mapper.Map<ConfigPublishHistoryDto>(publishHistory);
    }

    /// <summary>
    /// 创建发布历史记录
    /// </summary>
    public async Task<ConfigPublishHistoryDto> CreatePublishHistoryAsync(CreateConfigPublishHistoryDto createDto)
    {
        if (createDto.ConfigItems == null || !createDto.ConfigItems.Any())
        {
            throw new AppServiceException(400, "没有需要发布的配置项");
        }

        try
        {
            // 获取最新版本号
            long latestVersion = await _publishHistoryRepository.Find(h =>
                h.AppId == createDto.AppId &&
                h.Environment == createDto.Environment)
                .OrderByDescending(h => h.Version)
                .Select(h => h.Version)
                .FirstOrDefaultAsync();

            // 创建发布历史实体
            var publishHistory = Mapper.Map<ConfigPublishHistory>(createDto);
            publishHistory.Version = latestVersion + 1;

            // 添加实体并保存
            await _publishHistoryRepository.AddAsync(publishHistory, true);

            // 获取配置项ID列表
            var configItemIds = createDto.ConfigItems.Select(c => c.Id).ToList();

            // 批量获取现有配置项值
            var existingItems = await _configItemRepository
                .Find(ci => configItemIds.Contains(ci.Id))
                .ToDictionaryAsync(ci => ci.Id, ci => ci.Value);

            // 创建配置项发布历史
            var historyList = new List<ConfigItemPublishHistory>();
            foreach (var configItem in createDto.ConfigItems)
            {
                // 获取发布前的配置值
                var previousValue = existingItems.TryGetValue(configItem.Id, out var value)
                    ? value
                    : string.Empty;

                var configItemHistory = new ConfigItemPublishHistory
                {
                    ConfigPublishHistoryId = publishHistory.Id,
                    ConfigItemId = configItem.Id,
                    OldValue = previousValue,
                    NewValue = configItem.Value,
                    Version = configItem.Version
                };

                historyList.Add(configItemHistory);
            }

            // 批量添加历史记录
            foreach (var history in historyList)
            {
                await _configItemHistoryRepository.AddAsync(history, false);
            }

            // 保存所有更改
            await _configItemHistoryRepository.SaveChangesAsync();

            return Mapper.Map<ConfigPublishHistoryDto>(publishHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建发布历史记录失败: {AppId}/{Environment}", createDto.AppId, createDto.Environment);
            throw new AppServiceException(500, "创建发布历史记录失败");
        }
    }

    /// <summary>
    /// 手动创建发布历史记录 (原有API兼容方法)
    /// </summary>
    public async Task<ConfigPublishHistory> CreatePublishHistoryAsync(string appId, string environment, string description, IEnumerable<ConfigItem> configItems)
    {
        if (configItems == null || !configItems.Any())
        {
            throw new AppServiceException(400, "没有需要发布的配置项");
        }

        try
        {
            // 获取最新版本号
            long latestVersion = await _publishHistoryRepository.Find(h =>
                h.AppId == appId &&
                h.Environment == environment)
                .OrderByDescending(h => h.Version)
                .Select(h => h.Version)
                .FirstOrDefaultAsync();

            // 创建发布历史
            var publishHistory = new ConfigPublishHistory
            {
                AppId = appId,
                Environment = environment,
                Description = description ?? string.Empty,
                Version = latestVersion + 1
            };

            // 添加发布历史记录
            await _publishHistoryRepository.AddAsync(publishHistory, true);

            // 获取配置项ID列表
            var configItemIds = configItems.Select(c => c.Id).ToList();

            // 批量获取现有配置项值
            var existingItems = await _configItemRepository
                .Find(ci => configItemIds.Contains(ci.Id))
                .ToDictionaryAsync(ci => ci.Id, ci => ci.Value);

            // 创建配置项发布历史
            var historyList = new List<ConfigItemPublishHistory>();
            foreach (var configItem in configItems)
            {
                // 获取发布前的配置值
                var previousValue = existingItems.TryGetValue(configItem.Id, out var value)
                    ? value
                    : string.Empty;

                var configItemHistory = new ConfigItemPublishHistory
                {
                    ConfigPublishHistoryId = publishHistory.Id,
                    ConfigItemId = configItem.Id,
                    OldValue = previousValue,
                    NewValue = configItem.Value,
                    Version = configItem.Version
                };

                historyList.Add(configItemHistory);
            }

            // 批量添加历史记录
            foreach (var history in historyList)
            {
                await _configItemHistoryRepository.AddAsync(history, false);
            }

            // 保存所有更改
            await _configItemHistoryRepository.SaveChangesAsync();

            return publishHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建发布历史记录失败: {AppId}/{Environment}", appId, environment);
            throw; // 重新抛出异常，确保调用方的事务回滚
        }
    }

    /// <summary>
    /// 回滚到指定的发布历史
    /// </summary>
    public async Task<(bool success, string message)> RollbackToHistoryAsync(int publishHistoryId)
    {
        try
        {
            // 获取发布历史详情
            var publishHistoryDto = await GetPublishHistoryDetailAsync(publishHistoryId);
            if (publishHistoryDto == null)
            {
                return (false, "发布历史记录不存在");
            }

            // 获取所有配置项发布历史
            var configItemHistories = await _configItemHistoryRepository
                .Find(h => h.ConfigPublishHistoryId == publishHistoryId)
                .ToListAsync();

            if (!configItemHistories.Any())
            {
                return (false, "没有可回滚的配置项");
            }

            int successCount = 0;
            var failedItems = new List<string>();

            // 使用事务处理回滚
            await _configItemRepository.ExecuteInTransactionAsync(async () =>
            {
                // 回滚每个配置项
                foreach (var history in configItemHistories)
                {
                    try
                    {
                        var configItem = await _configItemRepository.GetByIdAsync(history.ConfigItemId);
                        if (configItem == null)
                        {
                            failedItems.Add($"配置项(ID={history.ConfigItemId})不存在");
                            continue;
                        }

                        // 更新配置项为历史值
                        configItem.Value = history.OldValue;
                        configItem.Status = ConfigStatus.Editing; // 回滚后默认为编辑状态
                        configItem.Version++; // 增加版本号

                        await _configItemRepository.UpdateAsync(configItem);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "回滚配置项失败: {ConfigItemId}", history.ConfigItemId);
                        failedItems.Add($"配置项(ID={history.ConfigItemId})回滚失败: {ex.Message}");
                    }
                }
            });

            // 事务完成后，清除缓存和发送通知
            if (successCount > 0)
            {
                // 从第一个历史项获取应用和环境信息
                var firstHistory = configItemHistories.First();
                var configItem = await _configItemRepository.GetByIdAsync(firstHistory.ConfigItemId);
                if (configItem != null)
                {
                    var appId = configItem.AppId;
                    var environment = configItem.Environment.ToString();

                    // 发送配置变更通知
                    await _notificationService.NotifyConfigChangedAsync(appId, environment);
                }
            }

            if (failedItems.Any())
            {
                return (successCount > 0, $"部分配置项回滚成功: {successCount}个成功, {failedItems.Count}个失败");
            }
            else
            {
                return (true, $"回滚成功，共{successCount}个配置项");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚发布历史失败: {PublishHistoryId}", publishHistoryId);
            return (false, $"回滚失败: {ex.Message}");
        }
    }

    #region Override BaseService Methods
    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateConfigPublishHistoryDto createDto)
    {
        if (string.IsNullOrEmpty(createDto.AppId))
        {
            throw new AppServiceException(400, "应用ID不能为空");
        }

        if (string.IsNullOrEmpty(createDto.Environment))
        {
            throw new AppServiceException(400, "环境不能为空");
        }

        // 可添加其他验证逻辑
        await Task.CompletedTask;
    }

    #endregion
}