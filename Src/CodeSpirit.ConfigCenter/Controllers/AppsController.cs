using AutoMapper;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 应用管理控制器
/// </summary>
[DisplayName("应用管理")]
[Page(Label = "应用管理", ParentLabel = "配置中心", Icon = "fa-solid fa-apps", PermissionCode = PermissionCodes.AppManagement)]
[Permission(code: PermissionCodes.AppManagement)]
public class AppsController : ApiControllerBase
{
    private readonly IAppService _appService;
    private readonly IMapper _mapper;
    private readonly ILogger<AppsController> _logger;

    /// <summary>
    /// 初始化应用管理控制器
    /// </summary>
    /// <param name="appService">应用服务</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    public AppsController(
        IAppService appService,
        IMapper mapper,
        ILogger<AppsController> logger)
    {
        ArgumentNullException.ThrowIfNull(appService);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(logger);

        _appService = appService;
        _mapper = mapper;
        _logger = logger;
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
    [Operation("配置管理", "link", "/config/settings?appId=${id}", null)]
    public ActionResult<ApiResponse> ManageSettings()
    {
        return SuccessResponse();
    }

    [Operation(label: "批量配置", actionType: "service")]
    [HttpGet("batch/settings")]
    public JObject CreateBatchConfigButton(string id)
    {
        JObject tabs = new JObject
        {
            ["type"] = "tabs",
            ["tabs"] = new JArray
            {
                new JObject
                {
                    ["title"] = "开发环境",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["initApi"]="get:${ROOT_API}/api/config/ConfigItems/${id}/Development/collection",
                        ["api"] = "put:${ROOT_API}/api/config/ConfigItems/${id}/Development/collection",
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
                },
                new JObject
                {
                    ["title"] = "预发布环境",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["initApi"]="get:${ROOT_API}/api/config/ConfigItems/${id}/Staging/collection",
                        ["api"] = "put:${ROOT_API}/api/config/ConfigItems/${id}/Staging/collection",
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
                },
                new JObject
                {
                    ["title"] = "生产环境",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["initApi"]="get:${ROOT_API}/api/config/ConfigItems/${id}/Production/collection",
                        ["api"] = "put:${ROOT_API}/api/config/ConfigItems/${id}/Production/collection",
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
                }
            }
        };

        return tabs;
    }
}