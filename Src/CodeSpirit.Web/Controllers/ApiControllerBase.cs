using CodeSpirit.Amis.Controllers;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Web.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// API控制器基类
    /// </summary>
    [ApiController]
    [Authorize(policy: "DynamicPermissions")]
    [Route("api/web/[controller]")]
    [Module(Constants.Constants.Module, "系统管理", Icon = "fa-solid fa-gear")]
    public abstract class ApiControllerBase : AmisApiControllerBase
    {
        /// <summary>
        /// 生成成功响应
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">返回的数据</param>
        /// <param name="msg">成功消息</param>
        /// <returns>统一格式的成功响应</returns>
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string msg = "操作成功！") where T : class
        {
            return Ok(new ApiResponse<T>(0, msg, data));
        }

        /// <summary>
        /// 生成成功响应（无数据）
        /// </summary>
        /// <param name="msg">成功消息</param>
        /// <returns>统一格式的成功响应</returns>
        protected ActionResult<ApiResponse> SuccessResponse(string msg = "操作成功！")
        {
            return Ok(new ApiResponse(0, msg));
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
        /// 生成失败响应（无数据）
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
} 