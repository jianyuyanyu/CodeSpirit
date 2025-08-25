using CodeSpirit.Amis.Controllers;
using CodeSpirit.Authorization;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷系统API控制器基类
/// </summary>
[ApiController]
[Authorize(policy: "DynamicPermissions")]
[Route("api/survey/[controller]")]
[Module("Survey", "问卷调查", Icon = "fa-solid fa-poll")]
[Navigation(Icon = "fa-solid fa-poll", PlatformType = PlatformType.Tenant)]
public abstract class ApiControllerBase : AmisApiControllerBase
{
    /// <summary>
    /// 生成成功响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">返回的数据</param>
    /// <returns>统一格式的成功响应</returns>
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T? data = default) where T : class
    {
        return Ok(new ApiResponse<T>(0, "操作成功！", data));
    }

    /// <summary>
    /// 生成成功响应
    /// </summary>
    /// <param name="msg">成功消息</param>
    /// <returns>统一格式的成功响应</returns>
    protected ActionResult<ApiResponse> SuccessResponse(string msg = "操作成功！")
    {
        return Ok(new ApiResponse(0, msg));
    }

    /// <summary>
    /// 生成创建成功响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="actionName">操作名称</param>
    /// <param name="data">返回的数据</param>
    /// <returns>统一格式的创建成功响应</returns>
    protected ActionResult<ApiResponse<T>> SuccessResponseWithCreate<T>(string actionName, T? data = default) where T : class
    {
        ApiResponse<T> response = new(0, "创建成功！", data);
        return CreatedAtAction(actionName, data, response);
    }

    /// <summary>
    /// 生成失败响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <param name="code">错误代码（默认1）</param>
    /// <param name="statusCode">HTTP状态码（默认400）</param>
    /// <returns>统一格式的错误响应</returns>
    protected ActionResult<ApiResponse<T>> BadResponse<T>(string message = "操作失败！", int code = 1, int statusCode = 400) where T : class
    {
        return StatusCode(statusCode, new ApiResponse<T>(code, message, null));
    }

    /// <summary>
    /// 生成失败响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">错误代码（默认1）</param>
    /// <param name="statusCode">HTTP状态码（默认400）</param>
    /// <returns>统一格式的错误响应</returns>
    protected ActionResult<ApiResponse> BadResponse(string message = "操作失败！", int code = 1, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse(code, message));
    }
}
