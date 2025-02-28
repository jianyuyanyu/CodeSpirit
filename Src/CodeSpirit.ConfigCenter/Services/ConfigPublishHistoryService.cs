using AutoMapper;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置发布历史服务实现
/// </summary>
public class ConfigPublishHistoryService : IConfigPublishHistoryService
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
        ILogger<ConfigPublishHistoryService> logger)
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
    public async Task<PageList<ConfigPublishHistory>> GetPublishHistoryListAsync(string appId, string environment, int pageIndex = 1, int pageSize = 20)
    {
        try
        {
            // 构建查询
            var query = _publishHistoryRepository.Find(h => 
                h.AppId == appId && 
                h.Environment == environment)
                .OrderByDescending(h => h.CreatedAt);

            // 分页
            int totalCount = await query.CountAsync();
            var publishHistories = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PageList<ConfigPublishHistory>(publishHistories, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发布历史列表失败: {AppId}/{Environment}", appId, environment);
            throw new AppServiceException(500, "获取发布历史列表失败");
        }
    }

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    public async Task<ConfigPublishHistory> GetPublishHistoryDetailAsync(int publishHistoryId)
    {
        try
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

            return publishHistory;
        }
        catch (AppServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发布历史详情失败: {PublishHistoryId}", publishHistoryId);
            throw new AppServiceException(500, "获取发布历史详情失败");
        }
    }

    /// <summary>
    /// 创建发布历史记录
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

            // 使用事务确保操作的原子性
            await _publishHistoryRepository.ExecuteInTransactionAsync(async () =>
            {
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

                    // 添加到集合，稍后批量保存
                    historyList.Add(configItemHistory);
                    
                    // 更新配置项状态为已发布
                    if (existingItems.ContainsKey(configItem.Id))
                    {
                        var item = await _configItemRepository.GetByIdAsync(configItem.Id);
                        item.Status = ConfigStatus.Released;
                        await _configItemRepository.UpdateAsync(item, false);
                    }
                }

                // 批量添加历史记录
                foreach (var history in historyList)
                {
                    await _configItemHistoryRepository.AddAsync(history, false);
                }

                // 保存所有更改
                await _configItemHistoryRepository.SaveChangesAsync();
                
            });
            return publishHistory;
        }
        catch (AppServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建发布历史记录失败: {AppId}/{Environment}", appId, environment);
            throw new AppServiceException(500, "创建发布历史记录失败");
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
            var publishHistory = await GetPublishHistoryDetailAsync(publishHistoryId);
            if (publishHistory == null)
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

            // 回滚每个配置项
            foreach (var history in configItemHistories)
            {
                try
                {
                    var configItem = await _configItemRepository.GetByIdAsync(history.ConfigItemId);
                    if (configItem == null)
                    {
                        // 如果配置项不存在，可能是已被删除，可以选择跳过或创建新配置
                        failedItems.Add($"配置项(ID={history.ConfigItemId})不存在");
                        continue;
                    }

                    // 更新配置项为历史值
                    configItem.Value = history.OldValue;
                    configItem.Status = ConfigStatus.Editing; // 回滚后默认为编辑状态
                    configItem.Version++; // 增加版本号

                    await _configItemRepository.UpdateAsync(configItem);

                    // 清除缓存
                    string cacheKey = $"config:{configItem.AppId}:{configItem.Environment}:{configItem.Key}";
                    await _cacheService.RemoveAsync(cacheKey);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "回滚配置项失败: {ConfigItemId}", history.ConfigItemId);
                    failedItems.Add($"配置项(ID={history.ConfigItemId})回滚失败: {ex.Message}");
                }
            }

            await _configItemRepository.SaveChangesAsync();

            // 发送配置变更通知
            await _notificationService.NotifyConfigChangedAsync(
                publishHistory.AppId, publishHistory.Environment);

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
} 