//namespace CodeSpirit.ConfigCenter.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;

///// <summary>
///// 配置发布管理服务实现
///// </summary>
//public class PublishService : IPublishService
//{
//    private readonly IPublishRepository _repository;
//    private readonly IConfigItemRepository _configRepository;
//    private readonly IConfigCacheService _cacheService;
//    private readonly IConfigChangeNotifier _notifier;
//    private readonly ILogger<PublishService> _logger;

//    public PublishService(
//        IPublishRepository repository,
//        IConfigItemRepository configRepository,
//        IConfigCacheService cacheService,
//        IConfigChangeNotifier notifier,
//        ILogger<PublishService> logger)
//    {
//        _repository = repository;
//        _configRepository = configRepository;
//        _cacheService = cacheService;
//        _notifier = notifier;
//        _logger = logger;
//    }

//    public async Task<PublishDetail> GetLatestPublishAsync(string appId, string environment)
//    {
//        try
//        {
//            return await _repository.GetLatestPublishAsync(appId, environment);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "获取最新发布记录失败: {AppId}/{Environment}", appId, environment);
//            throw new AppServiceException(500, "获取最新发布记录失败");
//        }
//    }

//    public async Task<PageList<PublishDetail>> GetPublishHistoryAsync(PublishQueryDto queryDto)
//    {
//        try
//        {
//            var (histories, total) = await _repository.GetPublishHistoryAsync(
//                queryDto.AppId,
//                queryDto.Environment,
//                queryDto.PageIndex,
//                queryDto.PageSize,
//                queryDto.Sorting);

//            return new ListData<PublishDetail>(histories, total);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "获取发布历史失败");
//            throw new AppServiceException(500, "获取发布历史失败");
//        }
//    }

//    public async Task<PublishDetail> PublishConfigAsync(string appId, string environment, string description, string publishBy)
//    {
//        try
//        {
//            // 获取待发布的配置
//            var configs = await _configRepository.GetConfigsAsync(
//                appId, environment, null, null, "Modified", 1, int.MaxValue, null);

//            if (!configs.Items.Any())
//            {
//                throw new AppServiceException(400, "没有需要发布的配置");
//            }

//            // 创建发布记录
//            var publish = new PublishDetail
//            {
//                AppId = appId,
//                Environment = environment,
//                ConfigSnapshot = JsonConvert.SerializeObject(configs.Items),
//                Description = description,
//                PublishBy = publishBy,
//                PublishTime = DateTime.UtcNow
//            };

//            var result = await _repository.CreatePublishAsync(publish);

//            // 更新配置状态
//            foreach (var config in configs.Items)
//            {
//                config.Status = "Published";
//                await _configRepository.UpdateConfigAsync(config);
//                await _cacheService.RemoveAsync($"config:{config.AppId}:{config.Environment}:{config.Key}");
//            }

//            // 通知配置变更
//            await _notifier.NotifyConfigChangedAsync(appId, environment);

//            return result;
//        }
//        catch (AppServiceException)
//        {
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "发布配置失败: {AppId}/{Environment}", appId, environment);
//            throw new AppServiceException(500, "发布配置失败");
//        }
//    }

//    public async Task<PublishDetail> RollbackAsync(long publishId, string rollbackBy)
//    {
//        try
//        {
//            var result = await _repository.RollbackAsync(publishId);

//            // 从快照恢复配置
//            var configs = JsonConvert.DeserializeObject<List<ConfigItem>>(result.ConfigSnapshot);
//            foreach (var config in configs)
//            {
//                await _configRepository.UpdateConfigAsync(config);
//                await _cacheService.RemoveAsync($"config:{config.AppId}:{config.Environment}:{config.Key}");
//            }

//            // 通知配置变更
//            await _notifier.NotifyConfigChangedAsync(result.AppId, result.Environment);

//            return result;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "回滚配置失败: {PublishId}", publishId);
//            throw new AppServiceException(500, "回滚配置失败");
//        }
//    }
//} 