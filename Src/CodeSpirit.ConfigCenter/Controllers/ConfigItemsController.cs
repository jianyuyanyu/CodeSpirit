using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 配置项管理控制器
/// </summary>
[DisplayName("配置项管理")]
[Page(Label = "配置项管理", ParentLabel = "配置中心", Icon = "fa-solid fa-gear", PermissionCode = PermissionCodes.ConfigItemManagement)]
[Permission(code: PermissionCodes.ConfigItemManagement)]
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
} 