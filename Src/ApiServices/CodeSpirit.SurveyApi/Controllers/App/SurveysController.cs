using CodeSpirit.Core;
using CodeSpirit.SurveyApi.Dtos.App;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Controllers.App;

/// <summary>
/// App端问卷控制器
/// </summary>
[DisplayName("App问卷")]
public class SurveysController : AppControllerBase
{
    private readonly IAppSurveyService _appSurveyService;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// 初始化App端问卷控制器
    /// </summary>
    /// <param name="appSurveyService">App端问卷服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveysController(
        IAppSurveyService appSurveyService,
        ILogger<SurveysController> logger)
    {
        ArgumentNullException.ThrowIfNull(appSurveyService);
        ArgumentNullException.ThrowIfNull(logger);

        _appSurveyService = appSurveyService;
        _logger = logger;
    }

    /// <summary>
    /// 获取公开问卷列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>公开问卷列表</returns>
    [HttpGet("public")]
    [DisplayName("获取公开问卷列表")]
    public async Task<ActionResult<ApiResponse<List<AppSurveyDto>>>> GetPublicSurveys([FromQuery] AppSurveyQueryDto queryDto)
    {
        try
        {
            var result = await _appSurveyService.GetPublicSurveysAsync(queryDto);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取公开问卷列表失败，查询参数: {@QueryDto}", queryDto);
            return ErrorResponse<List<AppSurveyDto>>("获取问卷列表失败");
        }
    }

    /// <summary>
    /// 根据ID获取问卷详情
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取问卷详情")]
    public async Task<ActionResult<ApiResponse<AppSurveyDetailDto>>> GetSurvey(int id)
    {
        try
        {
            var result = await _appSurveyService.GetSurveyAsync(id);
            
            if (result == null)
            {
                return ErrorResponse<AppSurveyDetailDto>("问卷不存在");
            }

            return SuccessResponse(result);
        }
        catch (UnauthorizedAccessException)
        {
            return ErrorResponse<AppSurveyDetailDto>("问卷不可访问");
        }
        catch (InvalidOperationException ex)
        {
            return ErrorResponse<AppSurveyDetailDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问卷详情失败，问卷ID: {SurveyId}", id);
            return ErrorResponse<AppSurveyDetailDto>("获取问卷详情失败");
        }
    }

    /// <summary>
    /// 检查问卷是否可以参与
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>检查结果</returns>
    [HttpGet("{id}/check")]
    [DisplayName("检查问卷可用性")]
    public async Task<ActionResult<ApiResponse<object>>> CheckSurveyAvailability(int id)
    {
        try
        {
            var result = await _appSurveyService.CheckSurveyAvailabilityAsync(id);
            
            if (result == null)
            {
                return ErrorResponse<object>("问卷不存在");
            }

            return SuccessResponse<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查问卷可用性失败，问卷ID: {SurveyId}", id);
            return ErrorResponse<object>("检查问卷可用性失败");
        }
    }

    /// <summary>
    /// 根据公开访问码获取问卷详情
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>问卷详情</returns>
    [HttpGet("public/{accessCode}")]
    [DisplayName("根据访问码获取问卷详情")]
    public async Task<ActionResult<ApiResponse<AppSurveyDetailDto>>> GetSurveyByAccessCode(string accessCode)
    {
        try
        {
            var result = await _appSurveyService.GetSurveyByAccessCodeAsync(accessCode);
            
            if (result == null)
            {
                return ErrorResponse<AppSurveyDetailDto>("问卷不存在或访问码无效");
            }

            return SuccessResponse(result);
        }
        catch (UnauthorizedAccessException)
        {
            return ErrorResponse<AppSurveyDetailDto>("问卷不可访问");
        }
        catch (InvalidOperationException ex)
        {
            return ErrorResponse<AppSurveyDetailDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据访问码获取问卷详情失败，访问码: {AccessCode}", accessCode);
            return ErrorResponse<AppSurveyDetailDto>("获取问卷详情失败");
        }
    }

    /// <summary>
    /// 根据公开访问码检查问卷是否可以参与
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>检查结果</returns>
    [HttpGet("public/{accessCode}/check")]
    [DisplayName("根据访问码检查问卷可用性")]
    public async Task<ActionResult<ApiResponse<object>>> CheckSurveyAvailabilityByAccessCode(string accessCode)
    {
        try
        {
            var result = await _appSurveyService.CheckSurveyAvailabilityByAccessCodeAsync(accessCode);
            
            if (result == null)
            {
                return ErrorResponse<object>("问卷不存在或访问码无效");
            }

            return SuccessResponse<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据访问码检查问卷可用性失败，访问码: {AccessCode}", accessCode);
            return ErrorResponse<object>("检查问卷可用性失败");
        }
    }

    /// <summary>
    /// 获取问卷分类选项
    /// </summary>
    /// <returns>分类选项列表</returns>
    [HttpGet("categories")]
    [DisplayName("获取问卷分类选项")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetSurveyCategories()
    {
        try
        {
            var result = await _appSurveyService.GetSurveyCategoriesAsync();
            return SuccessResponse<List<object>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问卷分类选项失败");
            return ErrorResponse<List<object>>("获取分类选项失败");
        }
    }
}
