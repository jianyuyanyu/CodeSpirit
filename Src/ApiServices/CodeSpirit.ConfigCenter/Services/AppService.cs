#nullable enable
using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Dtos.Common;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace CodeSpirit.ConfigCenter.Services;

    /// <summary>
    /// 应用管理服务实现
    /// </summary>
    public class AppService : BaseCRUDIService<App, AppDto, string, CreateAppDto, UpdateAppDto, AppBatchImportItemDto>, IAppService
    {
        private readonly IAppHealthService _appHealthService;
        private readonly IConfigPublishHistoryService _publishHistoryService;

        /// <summary>
        /// 初始化应用管理服务
        /// </summary>
        /// <param name="repository">应用仓储</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="importHelper">批量导入助手</param>
        /// <param name="appHealthService">应用健康服务</param>
        /// <param name="publishHistoryService">发布历史服务</param>
        public AppService(
            IRepository<App> repository, 
            IMapper mapper,
            EnhancedBatchImportHelper<AppBatchImportItemDto> importHelper,
            IAppHealthService appHealthService,
            IConfigPublishHistoryService publishHistoryService)
            : base(repository, mapper, importHelper)
        {
            _appHealthService = appHealthService;
            _publishHistoryService = publishHistoryService;
        }

    /// <summary>
    /// 获取指定应用信息
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>应用信息DTO</returns>
    public async Task<AppDto?> GetAppAsync(string appId)
    {
        var appDto = await GetAsync(appId);
        if (appDto != null)
        {
            await FillHealthStatusAsync(appDto);
            await FillConfigCountAsync(appDto);
            await FillConfigVersionAsync(appDto);
        }
        return appDto;
    }

    /// <summary>
    /// 获取应用分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>应用信息分页列表</returns>
    public async Task<PageList<AppDto>> GetAppsAsync(AppQueryDto queryDto)
    {
        ExpressionStarter<App> predicate = PredicateBuilder.New<App>(true);

        if (!string.IsNullOrEmpty(queryDto.AppId))
        {
            predicate = predicate.And(x => x.Id == queryDto.AppId);
        }
        if (!string.IsNullOrEmpty(queryDto.Name))
        {
            predicate = predicate.And(x => x.Name.Contains(queryDto.Name));
        }
        if (queryDto.Enabled.HasValue)
        {
            predicate = predicate.And(x => x.Enabled == queryDto.Enabled.Value);
        }
        if (!string.IsNullOrEmpty(queryDto.Tag))
        {
            predicate = predicate.And(x => x.Tag == queryDto.Tag);
        }

        var result = await GetPagedListAsync(
            queryDto,
            predicate,
            "InheritancedApp",
            "ConfigItems"
        );

        // 填充健康状态、配置数和配置版本
        if (result != null && result.Items != null)
        {
            foreach (var appDto in result.Items)
            {
                await FillHealthStatusAsync(appDto);
                await FillConfigCountAsync(appDto);
                await FillConfigVersionAsync(appDto);
            }
        }

        return result ?? new PageList<AppDto>();
    }

    /// <summary>
    /// 创建新应用
    /// </summary>
    /// <param name="appDto">创建应用DTO</param>
    /// <returns>创建的应用实体</returns>
    public async Task<AppDto> CreateAppAsync(CreateAppDto appDto)
    {
        return await CreateAsync(appDto);
    }

    /// <summary>
    /// 更新应用信息
    /// </summary>
    /// <param name="appDto">更新应用DTO</param>
    public async Task UpdateAppAsync(string id,UpdateAppDto appDto)
    {
        await UpdateAsync(id, appDto);
    }

    /// <summary>
    /// 删除应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    public async Task DeleteAppAsync(string appId)
    {
        await DeleteAsync(appId);
    }

    /// <summary>
    /// 验证应用密钥
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="secret">密钥</param>
    /// <returns>验证是否通过</returns>
    public async Task<bool> ValidateAppSecretAsync(string appId, string secret)
    {
        AppDto app = await GetAsync(appId);
        return app?.Secret == secret;
    }

    /// <summary>
    /// 批量导入应用
    /// </summary>
    /// <param name="importData">导入数据集合</param>
    /// <returns>导入结果，包含成功数量和失败的应用ID列表</returns>
    public async Task<(int successCount, List<string> failedAppIds)> BatchImportAppsAsync(
        IEnumerable<AppBatchImportItemDto> importData)
    {
        return await BatchImportAsync(importData);
    }

    /// <summary>
    /// 批量删除应用
    /// </summary>
    /// <param name="appIds">要删除的应用ID集合</param>
    /// <returns>删除结果，包含成功数量和失败的应用ID列表</returns>
    public async Task<(int successCount, List<string> failedAppIds)> BatchDeleteAppsAsync(IEnumerable<string> appIds)
    {
        (int successCount, List<string> failedIds) result = await BatchDeleteAsync(appIds);
        return (result.successCount, result.failedIds.Select(x => x.ToString()).ToList());
    }

    /// <summary>
    /// 快速保存应用信息
    /// </summary>
    /// <param name="request">快速保存请求数据</param>
    public async Task QuickSaveAppsAsync(QuickSaveRequestDto request)
    {
        if (request?.Rows == null || !request.Rows.Any())
        {
            throw new AppServiceException(400, "请求数据无效或为空！");
        }

        // 获取需要更新的应用ID列表
        List<string> appIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
        List<App> appsToUpdate = await Repository.Find(x => appIdsToUpdate.Contains(x.Id)).ToListAsync();
        
        if (appsToUpdate.Count != appIdsToUpdate.Count)
        {
            throw new AppServiceException(400, "部分应用未找到!");
        }

        // 执行批量更新：更新 `rowsDiff` 中的变化字段
        foreach (var rowDiff in request.RowsDiff)
        {
            App? app = appsToUpdate.FirstOrDefault(a => a.Id == rowDiff.Id);
            if (app != null)
            {
                if (rowDiff.Enabled.HasValue)
                {
                    app.Enabled = rowDiff.Enabled.Value;
                }

                if (rowDiff.AutoPublish.HasValue)
                {
                    app.AutoPublish = rowDiff.AutoPublish.Value;
                }

                await Repository.UpdateAsync(app, false);
            }
        }

        await Repository.SaveChangesAsync();
    }

    #region Override Base Methods

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <exception cref="AppServiceException">当应用ID已存在时抛出异常</exception>
    protected override async Task ValidateCreateDto(CreateAppDto createDto)
    {
        AppDto exists = await GetAsync(createDto.Id);
        if (exists != null)
        {
            throw new AppServiceException(400, "ID已存在！");
        }
    }

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <param name="updateDto">更新DTO</param>
    /// <exception cref="AppServiceException">当应用选择自己作为继承源或应用是自动注册时抛出异常</exception>
    protected override async Task ValidateUpdateDto(string id, UpdateAppDto updateDto)
    {
        if (updateDto.InheritancedAppId == id)
        {
            throw new AppServiceException(400, "应用不能选择自己作为继承源！");
        }

        // 检查应用是否是自动注册的
        var app = await Repository.GetByIdAsync(id);
        if (app != null && app.IsAutoRegistered)
        {
            throw new AppServiceException(400, "自动注册的应用不允许编辑！");
        }
    }

    /// <summary>
    /// 获取要更新的实体
    /// </summary>
    /// <param name="updateDto">更新DTO</param>
    /// <returns>待更新的应用实体</returns>
    protected override async Task<App> GetEntityForUpdate(string id, UpdateAppDto updateDto)
    {
        return await Repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    /// <param name="importDto">导入项DTO</param>
    /// <returns>导入项ID</returns>
    protected override string GetImportItemId(AppBatchImportItemDto importDto)
    {
        return importDto.Id;
    }

    /// <summary>
    /// 创建实体前的处理
    /// </summary>
    /// <param name="entity">待创建的应用实体</param>
    protected override Task OnCreating(App entity, CreateAppDto createAppDto)
    {
        entity.Secret = GenerateAppSecret();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 导入实体前的处理
    /// </summary>
    /// <param name="entity">待导入的应用实体</param>
    protected override Task OnImporting(App entity)
    {
        entity.Secret = GenerateAppSecret();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除实体前的处理
    /// </summary>
    /// <param name="entity">待删除的应用实体</param>
    protected override async Task OnDeleting(App entity)
    {
        // Check for existing config items
        bool hasConfigItems = await Repository.CreateQuery()
            .Where(x => x.Id == entity.Id)
            .SelectMany(x => x.ConfigItems)
            .AnyAsync();

        if (hasConfigItems)
        {
            throw new AppServiceException(400, "无法删除存在配置项的应用，请先删除所有配置项！");
        }

        // Check for published config items
        bool hasPublishedConfigItems = await Repository.CreateQuery()
            .Where(x => x.Id == entity.Id)
            .SelectMany(x => x.ConfigItems)
            .AnyAsync(x => x.Status == ConfigStatus.Released);

        if (hasPublishedConfigItems)
        {
            throw new AppServiceException(400, "无法删除存在已发布配置项的应用，请先取消发布或删除所有已发布的配置项！");
        }
    }

    #endregion

    /// <summary>
    /// 生成应用密钥
    /// </summary>
    /// <returns>生成的密钥字符串</returns>
    private static string GenerateAppSecret()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 填充健康状态
    /// </summary>
    /// <param name="appDto">应用DTO</param>
    private async Task FillHealthStatusAsync(AppDto appDto)
    {
        if (string.IsNullOrEmpty(appDto.Id))
        {
            return;
        }

        try
        {
            appDto.HealthStatus = await _appHealthService.GetHealthStatusAsync(appDto.Id);
        }
        catch (Exception)
        {
            // 记录错误但不影响主流程
            // 健康状态保持为 null（未知状态）
        }
    }

    /// <summary>
    /// 填充配置数量
    /// </summary>
    /// <param name="appDto">应用DTO</param>
    private async Task FillConfigCountAsync(AppDto appDto)
    {
        if (string.IsNullOrEmpty(appDto.Id))
        {
            return;
        }

        try
        {
            // 如果映射时已经填充了配置数（ConfigItems 已加载），则不需要再次查询
            // 注意：ConfigCount 为 0 也可能是真的没有配置项，所以不能简单地判断 > 0
            // 这里我们总是查询一次，确保数据准确
            var configCount = await Repository.CreateQuery()
                .Where(x => x.Id == appDto.Id)
                .SelectMany(x => x.ConfigItems)
                .CountAsync();

            appDto.ConfigCount = configCount;
        }
        catch (Exception)
        {
            // 记录错误但不影响主流程
            // 配置数保持为映射时的值或 0
        }
    }

    /// <summary>
    /// 填充配置版本号（从发布历史获取最新版本）
    /// </summary>
    /// <param name="appDto">应用DTO</param>
    private async Task FillConfigVersionAsync(AppDto appDto)
    {
        if (string.IsNullOrEmpty(appDto.Id))
        {
            return;
        }

        try
        {
            var version = await _publishHistoryService.GetLatestVersionAsync(appDto.Id);
            appDto.ConfigVersion = version;
        }
        catch (Exception)
        {
            // 记录错误但不影响主流程
            // 配置版本保持为默认值 0
        }
    }

    /// <summary>
    /// 获取应用选择列表（用于下拉选择）
    /// </summary>
    /// <param name="name">应用名称搜索关键词</param>
    /// <returns>应用列表</returns>
    public async Task<List<AppDto>> GetAppsForSelectAsync(string? name = null)
    {
        ExpressionStarter<App> predicate = PredicateBuilder.New<App>(true);

        // 支持按名称搜索
        if (!string.IsNullOrEmpty(name))
        {
            predicate = predicate.And(x => x.Name.Contains(name));
        }

        // 只获取启用的应用
        predicate = predicate.And(x => x.Enabled);

        var apps = await Repository.Find(predicate)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var appDtos = Mapper.Map<List<AppDto>>(apps);
        return appDtos;
    }
}