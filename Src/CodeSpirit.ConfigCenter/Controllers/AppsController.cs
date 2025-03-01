using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ConfigCenter.Dtos.Config;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 应用管理控制器
/// </summary>
[DisplayName("应用管理")]
[Navigation(Icon = "fa-solid fa-cube")]
public class AppsController : ApiControllerBase
{
    private readonly IAppService _appService;
    private readonly ILogger<AppsController> _logger;
    private readonly IConfigItemService _configItemService;

    /// <summary>
    /// 初始化应用管理控制器
    /// </summary>
    /// <param name="appService">应用服务</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    public AppsController(
        IAppService appService,
        ILogger<AppsController> logger,
        IConfigItemService configItemService)
    {
        ArgumentNullException.ThrowIfNull(appService);
        ArgumentNullException.ThrowIfNull(logger);

        _appService = appService;
        _logger = logger;
        _configItemService = configItemService;
    }

    /// <summary>
    /// 获取应用列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>应用列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<AppDto>>>> GetApps([FromQuery] AppQueryDto queryDto)
    {
        PageList<AppDto> apps = await _appService.GetAppsAsync(queryDto);
        return SuccessResponse(apps);
    }

    /// <summary>
    /// 获取应用详情
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>应用详细信息</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AppDto>>> GetApp(string id)
    {
        AppDto app = await _appService.GetAppAsync(id);
        return SuccessResponse(app);
    }

    /// <summary>
    /// 创建应用
    /// </summary>
    /// <param name="createAppDto">创建应用请求数据</param>
    /// <returns>创建的应用信息</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AppDto>>> CreateApp(CreateAppDto createAppDto)
    {
        AppDto appDto = await _appService.CreateAppAsync(createAppDto);
        return SuccessResponse(appDto);
    }

    /// <summary>
    /// 更新应用
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <param name="updateAppDto">更新应用请求数据</param>
    /// <returns>更新后的应用信息</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateApp(string id, UpdateAppDto updateAppDto)
    {
        await _appService.UpdateAppAsync(id, updateAppDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除应用
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此应用吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteApp(string id)
    {
        await _appService.DeleteAppAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量导入应用
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<AppBatchImportItemDto> importDto)
    {
        ArgumentNullException.ThrowIfNull(importDto);

        (int successCount, List<string> failedAppIds) = await _appService.BatchImportAppsAsync(importDto.ImportData);

        return failedAppIds.Any()
            ? SuccessResponse($"成功导入 {successCount} 个应用，但以下应用导入失败: {string.Join(", ", failedAppIds)}")
            : SuccessResponse($"成功导入 {successCount} 个应用！");
    }

    /// <summary>
    /// 批量删除应用
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchDeleteDto<string> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        (int successCount, List<string> failedAppIds) = await _appService.BatchDeleteAppsAsync(request.Ids);

        return failedAppIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个应用，但以下应用删除失败: {string.Join(", ", failedAppIds)}")
            : SuccessResponse($"成功删除 {successCount} 个应用！");
    }

    /// <summary>
    /// 配置管理（仅用于生成跳转操作）
    /// </summary>
    /// <returns>操作结果</returns>
    [Operation("配置管理", "link", "/config/configItems?appId=${id}", null)]
    public ActionResult<ApiResponse> ManageSettings()
    {
        return SuccessResponse();
    }

    [Operation("发布历史", "link", "/config/configPublishHistories?appId=${id}", null)]
    public ActionResult<ApiResponse> ConfigPublishHistories()
    {
        return SuccessResponse();
    }

    /// <summary>
    /// 获取批量配置表单定义
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>表单配置JSON对象</returns>
    [Operation(label: "批量配置", actionType: "service")]
    [HttpGet("batch/settings")]
    public JObject CreateBatchConfigButton(string id)
    {
        var tabsArray = new JArray();

        foreach (EnvironmentType envType in Enum.GetValues<EnvironmentType>())
        {
            var envName = envType.ToString();
            var displayName = envType.GetDisplayName() ?? envName;

            tabsArray.Add(new JObject
            {
                ["title"] = $"{displayName}（不含父级配置）",
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["title"] = "",
                    ["initApi"] = $"get:${{ROOT_API}}/api/config/ConfigItems/${{id}}/{envName}/collection",
                    ["api"] = $"put:${{ROOT_API}}/api/config/ConfigItems/${{id}}/{envName}/collection",
                    ["body"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "json-editor",
                            ["name"] = "configs",
                            ["language"] = "json",
                            ["placeholder"] = "请输入JSON格式的配置",
                            ["required"] = true
                        }
                    }
                }
            });
        }

        return new JObject
        {
            ["type"] = "tabs",
            ["tabs"] = tabsArray
        };
    }

    /// <summary>
    /// 总体配置查看
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/view")]
    [Operation(label: "配置查看", actionType: "return-form", null)]
    public async Task<ActionResult<ApiResponse<ConfigItemsExportDto>>> GetCompare(string id)
    {
        var result = await _configItemService.GetAppConfigsWithInheritanceAsync(id, environment: EnvironmentType.Development.ToString());
        return SuccessResponse(result);
    }
}