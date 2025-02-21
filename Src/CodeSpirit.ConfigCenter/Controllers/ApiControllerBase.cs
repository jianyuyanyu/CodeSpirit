using CodeSpirit.Amis.Controllers;
using CodeSpirit.Authorization;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ConfigCenter.Controllers
{
    [ApiController]
    //[Authorize(policy: "DynamicPermissions")]
    [Route("api/config/[controller]")]
    [Module("config", "配置中心")]
    public abstract class ApiControllerBase : AmisApiControllerBase
    {
        /// <summary>
        /// 生成成功响应
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">返回的数据</param>
        /// <returns>统一格式的成功响应</returns>
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data = default) where T : class
        {
            return Ok(new ApiResponse<T>(0, "操作成功！", data));
        }

        protected ActionResult<ApiResponse> SuccessResponse(string msg = "操作成功！")
        {
            return Ok(new ApiResponse(0, "操作成功！"));
        }

        protected ActionResult<ApiResponse<T>> SuccessResponseWithCreate<T>(string actionName, T data = default) where T : class
        {
            ApiResponse<T> response = new(0, "创建成功！", data);
            return CreatedAtAction(actionName, data, response);
        }

        /// <summary>
        /// 生成失败响应
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="code">错误代码（默认1）</param>
        /// <param name="statusCode">HTTP状态码（默认400）</param>
        /// <returns>统一格式的错误响应</returns>
        protected ActionResult<ApiResponse<T>> BadResponse<T>(string message = "操作失败！", int code = 1, int statusCode = 400) where T : class
        {
            return StatusCode(statusCode, new ApiResponse<T>(code, message, null));
        }

        protected ActionResult<ApiResponse> BadResponse(string message = "操作失败！")
        {
            return BadResponse(message: message);
        }
    }
}
