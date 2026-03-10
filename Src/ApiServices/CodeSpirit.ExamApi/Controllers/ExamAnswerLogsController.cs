using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Localization.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 答题日志控制器
/// </summary>
[DisplayName("答题日志")]
[Navigation(Icon = "fa-solid fa-file-lines", PlatformType = PlatformType.Tenant)]
public class ExamAnswerLogsController : ApiControllerBase
{
    private readonly IExamAnswerLogService _examAnswerLogService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examAnswerLogService">答题日志服务</param>
    /// <param name="localizer">本地化服务</param>
    public ExamAnswerLogsController(IExamAnswerLogService examAnswerLogService, IStringLocalizer<SharedResources> localizer)
    {
        _examAnswerLogService = examAnswerLogService;
        _localizer = localizer;
    }

    /// <summary>
    /// 获取答题日志列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>答题日志分页列表</returns>
    [HttpGet]
    [DisplayName("获取答题日志列表")]
    public async Task<ActionResult<ApiResponse<PageList<ExamAnswerLogDto>>>> GetExamAnswerLogs([FromQuery] ExamAnswerLogQueryDto queryDto)
    {
        var result = await _examAnswerLogService.GetPagedListAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 导出答题日志列表（前端导出，后端仅返回数据）
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>导出的答题日志列表</returns>
    [HttpGet("Export")]
    [DisplayName("导出答题日志列表")]
    public async Task<ActionResult<ApiResponse<PageList<ExamAnswerLogDto>>>> Export([FromQuery] ExamAnswerLogQueryDto queryDto)
    {
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        var result = await _examAnswerLogService.GetPagedListAsync(queryDto);

        return result.Items.Count == 0
            ? BadResponse<PageList<ExamAnswerLogDto>>(_localizer["Common.NoDataToExport"].Value)
            : SuccessResponse(result);
    }
}
