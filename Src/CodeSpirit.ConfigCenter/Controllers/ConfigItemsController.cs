using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Dtos.PublishHistory;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 配置项管理控制器
/// </summary>
[DisplayName("配置项管理")]
[Navigation(Icon = "fa-solid fa-gear")]
public class ConfigItemsController : ApiControllerBase
{
    private readonly IConfigItemService _configItemService;
    private readonly ILogger<ConfigItemsController> _logger;

    /// <summary>
    /// 初始化配置项管理控制器
    /// </summary>
    public ConfigItemsController(
        IConfigItemService configItemService,
        ILogger<ConfigItemsController> logger)
    {
        ArgumentNullException.ThrowIfNull(configItemService);
        ArgumentNullException.ThrowIfNull(logger);

        _configItemService = configItemService;
        _logger = logger;
    }

    /// <summary>
    /// 获取配置项列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>配置项列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ConfigItemDto>>>> GetConfigItems([FromQuery] ConfigItemQueryDto queryDto)
    {
        PageList<ConfigItemDto> configs = await _configItemService.GetConfigsAsync(queryDto);
        return SuccessResponse(configs);
    }

    /// <summary>
    /// 获取指定配置项
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="key">配置键</param>
    /// <returns>配置项详情</returns>
    [HttpGet("{appId}/{environment}/{key}")]
    public async Task<ActionResult<ApiResponse<ConfigItemDto>>> GetConfig(string appId, string environment, string key)
    {
        ConfigItemDto config = await _configItemService.GetConfigAsync(appId, environment, key);
        return SuccessResponse(config);
    }

    /// <summary>
    /// 创建配置项
    /// </summary>
    /// <param name="createDto">创建配置项请求数据</param>
    /// <returns>创建的配置项信息</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConfigItemDto>>> CreateConfig(CreateConfigDto createDto)
    {
        ConfigItemDto configDto = await _configItemService.CreateAsync(createDto);
        return SuccessResponseWithCreate(nameof(GetConfig), configDto);
    }

    /// <summary>
    /// 更新配置项
    /// </summary>
    /// <param name="id">配置项ID</param>
    /// <param name="updateDto">更新配置项请求数据</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateConfig(int id, UpdateConfigDto updateDto)
    {
        await _configItemService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除配置项
    /// </summary>
    /// <param name="id">配置项ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此配置项吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteConfig(int id)
    {
        await _configItemService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量导入配置项
    /// </summary>
    /// <param name="importDtos">导入的配置项列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse>> ImportConfigs([FromBody] List<ConfigItemBatchImportDto> importDtos)
    {
        await _configItemService.BatchImportAsync(importDtos);
        return SuccessResponse("导入成功");
    }

    /// <summary>
    /// 获取应用配置集合
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>应用配置集合</returns>
    [HttpGet("{appId}/{environment}/collection")]
    public async Task<ActionResult<ApiResponse<ConfigItemsExportDto>>> GetConfigCollection(string appId, string environment)
    {
        ConfigItemsExportDto configs = await _configItemService.GetAppConfigsAsync(appId, environment);
        return SuccessResponse(configs);
    }

    /// <summary>
    /// 批量更新应用配置
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="updateDto">更新请求数据</param>
    /// <returns>更新结果</returns>
    [HttpPut("{appId}/{environment}/collection")]
    public async Task<ActionResult<ApiResponse>> UpdateConfigCollection(string appId, string environment, [FromBody] ConfigItemsUpdateDto updateDto)
    {
        // 验证路由参数与请求体参数是否一致
        if (appId != updateDto.AppId || environment != updateDto.Environment)
        {
            return BadRequest("路由参数与请求体参数不一致");
        }

        (int successCount, List<string> failedKeys) = await _configItemService.UpdateConfigCollectionAsync(updateDto);

        if (failedKeys.Any())
        {
            return SuccessResponse($"成功更新 {successCount} 个配置，但以下配置更新失败: {string.Join(", ", failedKeys)}");
        }

        return SuccessResponse($"成功更新 {successCount} 个配置！");
    }

    /// <summary>
    /// 批量发布配置项
    /// </summary>
    /// <param name="publishDto">批量发布请求数据</param>
    /// <returns>发布结果</returns>
    [HttpPost("batch/publish")]
    [Operation("批量发布", "ajax", null, "确定要发布选中的配置项吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchPublishConfigs([FromBody] ConfigItemsBatchPublishDto publishDto)
    {
        ArgumentNullException.ThrowIfNull(publishDto);
        
        if (publishDto.Ids == null || !publishDto.Ids.Any())
        {
            return BadResponse("请选择要发布的配置项");
        }

        (int successCount, List<int> failedIds) = await _configItemService.BatchPublishAsync(publishDto);
        
        if (failedIds.Any())
        {
            return SuccessResponse($"成功发布 {successCount} 个配置，但以下配置ID发布失败: {string.Join(", ", failedIds)}");
        }

        return SuccessResponse($"成功发布 {successCount} 个配置！");
    }
}