using CodeSpirit.Core;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers.App;

/// <summary>
/// App端API控制器基类
/// </summary>
[ApiController]
[Route("api/app/[controller]")]
public abstract class AppControllerBase : ControllerBase
{
    /// <summary>
    /// 返回成功响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <returns>API响应</returns>
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data) where T : class
    {
        return Ok(ApiResponse<T>.Success(data));
    }

    /// <summary>
    /// 返回成功响应（无数据）
    /// </summary>
    /// <param name="message">响应消息</param>
    /// <returns>API响应</returns>
    protected ActionResult<ApiResponse> SuccessResponse(string message = "操作成功")
    {
        return Ok(ApiResponse.Success(message));
    }

    /// <summary>
    /// 返回错误响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>API响应</returns>
    protected ActionResult<ApiResponse> ErrorResponse(string message)
    {
        return BadRequest(ApiResponse.Error(400, message));
    }

    /// <summary>
    /// 返回错误响应（泛型版本）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>API响应</returns>
    protected ActionResult<ApiResponse<T>> ErrorResponse<T>(string message) where T : class
    {
        return BadRequest(ApiResponse<T>.Error(400, message));
    }
}
