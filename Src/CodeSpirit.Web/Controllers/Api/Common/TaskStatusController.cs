using System.ComponentModel;
using CodeSpirit.Core;
using CodeSpirit.Shared.Dtos.AI;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers.Api.Common;

/// <summary>
/// AI任务状态查询控制器
/// </summary>
/// <remarks>
/// 提供通用的AI任务状态查询API，供前端组件使用
/// </remarks>
[DisplayName("任务状态查询")]
[Route("api/common/[controller]")]
public class TaskStatusController : ApiControllerBase
{
    private readonly IAiTaskService _aiTaskService;

    /// <summary>
    /// 初始化任务状态控制器
    /// </summary>
    /// <param name="aiTaskService">AI任务服务</param>
    public TaskStatusController(IAiTaskService aiTaskService)
    {
        _aiTaskService = aiTaskService ?? throw new ArgumentNullException(nameof(aiTaskService));
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态信息</returns>
    [HttpGet("{taskId}")]
    [DisplayName("获取任务状态")]
    public async Task<ActionResult<ApiResponse<AiTaskStatusDto>>> GetTaskStatus([FromRoute]string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return BadRequest(ApiResponse<AiTaskStatusDto>.Error(400, "任务ID不能为空"));
        }

        try
        {
            var taskStatus = await _aiTaskService.GetTaskStatusAsync(taskId);
            
            if (taskStatus == null)
            {
                return NotFound(ApiResponse<AiTaskStatusDto>.Error(404, $"未找到任务：{taskId}"));
            }

            return Ok(ApiResponse<AiTaskStatusDto>.Success(taskStatus));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AiTaskStatusDto>.Error(500, $"获取任务状态失败：{ex.Message}"));
        }
    }
}
