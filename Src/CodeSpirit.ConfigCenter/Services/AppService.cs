using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Dtos.QueryDtos;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using System.Linq.Dynamic.Core;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 应用管理服务实现
/// </summary>
public class AppService : BaseService<App, AppDto, string, CreateAppDto, UpdateAppDto, AppBatchImportItemDto>, IAppService
{
    /// <summary>
    /// 初始化应用管理服务
    /// </summary>
    /// <param name="repository">应用仓储</param>
    /// <param name="mapper">对象映射器</param>
    public AppService(IRepository<App> repository, IMapper mapper) 
        : base(repository, mapper)
    {
    }

    /// <summary>
    /// 获取指定应用信息
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>应用信息DTO</returns>
    public async Task<AppDto> GetAppAsync(string appId)
    {
        return await GetAsync(appId);
    }

    /// <summary>
    /// 获取应用分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>应用信息分页列表</returns>
    public async Task<PageList<AppDto>> GetAppsAsync(AppQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<App>(true);

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

        return await GetPagedListAsync(
            queryDto,
            predicate
        );
    }

    /// <summary>
    /// 创建新应用
    /// </summary>
    /// <param name="appDto">创建应用DTO</param>
    /// <returns>创建的应用实体</returns>
    public async Task<App> CreateAppAsync(CreateAppDto appDto)
    {
        return await CreateAsync(appDto);
    }

    /// <summary>
    /// 更新应用信息
    /// </summary>
    /// <param name="appDto">更新应用DTO</param>
    public async Task UpdateAppAsync(UpdateAppDto appDto)
    {
        await UpdateAsync(appDto);
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
        var app = await GetAsync(appId);
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
        var result = await BatchDeleteAsync(appIds);
        return (result.successCount, result.failedIds.Select(x => x.ToString()).ToList());
    }

    #region Override Base Methods

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <exception cref="AppServiceException">当应用ID已存在时抛出异常</exception>
    protected override async Task ValidateCreateDto(CreateAppDto createDto)
    {
        var exists = await GetAsync(createDto.Id);
        if (exists != null)
        {
            throw new AppServiceException(400, "ID已存在！");
        }
    }

    /// <summary>
    /// 获取要更新的实体
    /// </summary>
    /// <param name="updateDto">更新DTO</param>
    /// <returns>待更新的应用实体</returns>
    protected override async Task<App> GetEntityForUpdate(UpdateAppDto updateDto)
    {
        return await Repository.GetByIdAsync(updateDto.Id);
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
    protected override Task OnCreating(App entity)
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

    #endregion

    /// <summary>
    /// 生成应用密钥
    /// </summary>
    /// <returns>生成的密钥字符串</returns>
    private static string GenerateAppSecret()
    {
        return Guid.NewGuid().ToString("N");
    }
}