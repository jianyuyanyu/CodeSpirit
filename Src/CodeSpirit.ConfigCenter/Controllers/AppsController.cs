using AutoMapper;
using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Dtos.QueryDtos;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

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
        var apps = await _appService.GetAppsAsync(queryDto);
        return SuccessResponse(apps);
    }

    /// <summary>
    /// 获取应用详情
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>应用详细信息</returns>
    [HttpGet("{appId}")]
    public async Task<ActionResult<ApiResponse<AppDto>>> GetApp(string appId)
    {
        var app = await _appService.GetAppAsync(appId);
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
        ArgumentNullException.ThrowIfNull(createAppDto);
        
        var app = await _appService.CreateAppAsync(createAppDto);
        var appDto = _mapper.Map<AppDto>(app);
        return SuccessResponseWithCreate(nameof(GetApp), appDto);
    }

    /// <summary>
    /// 更新应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="updateAppDto">更新应用请求数据</param>
    /// <returns>更新后的应用信息</returns>
    [HttpPut("{appId}")]
    public async Task<ActionResult<ApiResponse>> UpdateApp(string appId, UpdateAppDto updateAppDto)
    {
        ArgumentNullException.ThrowIfNull(updateAppDto);
        
        if (appId != updateAppDto.Id)
        {
            return BadResponse("应用ID不匹配");
        }
        await _appService.UpdateAppAsync(updateAppDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{appId}")]
    [Operation("删除", "ajax", null, "确定要删除此应用吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteApp(string appId)
    {
        await _appService.DeleteAppAsync(appId);
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
        
        var (successCount, failedAppIds) = await _appService.BatchImportAppsAsync(importDto.ImportData);
        
        if (failedAppIds.Any())
        {
            return SuccessResponse($"成功导入 {successCount} 个应用，但以下应用导入失败: {string.Join(", ", failedAppIds)}");
        }
        
        return SuccessResponse($"成功导入 {successCount} 个应用！");
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
        
        var (successCount, failedAppIds) = await _appService.BatchDeleteAppsAsync(request.Ids);

        if (failedAppIds.Any())
        {
            return SuccessResponse($"成功删除 {successCount} 个应用，但以下应用删除失败: {string.Join(", ", failedAppIds)}");
        }

        return SuccessResponse($"成功删除 {successCount} 个应用！");
    }
} 