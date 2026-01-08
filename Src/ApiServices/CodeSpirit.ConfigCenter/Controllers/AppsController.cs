using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using CodeSpirit.ConfigCenter.Dtos.Config;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 应用管理控制器
/// </summary>
[DisplayName("应用管理")]
[Navigation(Icon = "fa-solid fa-cube", PlatformType = PlatformType.Inherit)]
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
    [DisplayName("获取应用列表")]
    public async Task<ActionResult<ApiResponse<PageList<AppDto>>>> GetApps([FromQuery] AppQueryDto queryDto)
    {
        PageList<AppDto> apps = await _appService.GetAppsAsync(queryDto);
        return SuccessResponse(apps);
    }

    /// <summary>
    /// 获取应用选择列表（用于下拉选择，支持搜索）
    /// </summary>
    /// <param name="name">应用名称搜索关键词</param>
    /// <param name="term">搜索关键词（AMIS select 组件传递的参数）</param>
    /// <returns>应用列表</returns>
    [HttpGet("select")]
    [DisplayName("获取应用选择列表")]
    public async Task<ActionResult<ApiResponse<List<AppDto>>>> GetAppsForSelect([FromQuery] string? name = null, [FromQuery] string? term = null)
    {
        // 优先使用 term 参数（AMIS select 组件传递），如果没有则使用 name 参数
        string? searchKeyword = !string.IsNullOrEmpty(term) ? term : name;
        List<AppDto> apps = await _appService.GetAppsForSelectAsync(searchKeyword);
        return SuccessResponse(apps);
    }

    /// <summary>
    /// 获取应用详情
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>应用详细信息</returns>
    [HttpGet("{id}")]
    [DisplayName("获取应用详情")]
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
    [DisplayName("创建应用")]
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
    [DisplayName("更新应用")]
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
    [Operation("删除", "ajax", null, "确定要删除此应用吗？删除前将检查是否存在配置项或已发布的配置项。",visibleOn: "!isAutoRegistered")]
    public async Task<ActionResult<ApiResponse>> DeleteApp(string id)
    {
        await _appService.DeleteAppAsync(id);
        return SuccessResponse();
    }



    /// <summary>
    /// 批量删除应用
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true, visibleOn: "!isAutoRegistered")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<string> request)
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
    [Operation("配置管理", "link", "/config/configItems?appId=${id}", null, Icon = "fa-solid fa-gear")]
    [DisplayName("配置管理")]
    public ActionResult<ApiResponse> ManageSettings()
    {
        return SuccessResponse();
    }

    [Operation("发布历史", "link", "/config/configPublishHistories?appId=${id}", null, Icon = "fa-solid fa-clock-rotate-left")]
    [DisplayName("发布历史")]
    public ActionResult<ApiResponse> ConfigPublishHistories()
    {
        return SuccessResponse();
    }

    /// <summary>
    /// 获取批量配置表单定义
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>表单配置JSON对象</returns>
    [Operation(label: "批量配置", actionType: "service", Icon = "fa-solid fa-sliders")]
    [HttpGet("batch/settings")]
    [DisplayName("批量配置")]
    public JObject CreateBatchConfigButton(string id)
    {
        return new JObject
        {
            ["type"] = "form",
            ["title"] = "",
            ["initApi"] = $"get:${{ROOT_API}}/api/config/ConfigItems/${{id}}/collection",
            ["api"] = $"put:${{ROOT_API}}/api/config/ConfigItems/${{id}}/collection",
            ["body"] = new JArray
            {
                new JObject
                {
                    ["type"] = "json-editor",
                    ["name"] = "configs",
                    ["language"] = "json",
                    ["placeholder"] = "请输入JSON格式的配置。",
                    ["required"] = true
                }
            }
        };
    }
}