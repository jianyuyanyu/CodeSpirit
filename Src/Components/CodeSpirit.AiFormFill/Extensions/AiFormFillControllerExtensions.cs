using CodeSpirit.AiFormFill.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.AiFormFill.Extensions;

/// <summary>
/// AI表单填充控制器扩展方法
/// </summary>
public static class AiFormFillControllerExtensions
{
    /// <summary>
    /// 处理AI填充请求
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="controller">控制器</param>
    /// <param name="aiFormFillService">AI填充服务</param>
    /// <param name="request">请求对象</param>
    /// <returns>API响应</returns>
    public static async Task<ActionResult<ApiResponse<T>>> HandleAiFillAsync<T>(
        this ControllerBase controller,
        IAiFormFillService aiFormFillService,
        T request) where T : class, new()
    {
        try
        {
            // 验证DTO是否支持AI填充
            if (!aiFormFillService.IsAiFillSupported<T>())
            {
                return controller.BadRequest(ApiResponse.Error(400, $"类型 {typeof(T).Name} 不支持AI填充"));
            }

            // 获取触发字段值
            var triggerValue = GetTriggerValue(request);
            if (string.IsNullOrEmpty(triggerValue))
            {
                return controller.BadRequest(ApiResponse.Error(400, "触发字段值不能为空"));
            }

            // 执行AI填充
            var result = await aiFormFillService.FillFormAsync(triggerValue, request);
            
            return controller.Ok(ApiResponse<T>.Success(result, "AI填充完成"));
        }
        catch (BusinessException ex)
        {
            return controller.BadRequest(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception)
        {
            // 记录错误日志
            // 这里可以注入ILogger进行日志记录
            return controller.StatusCode(500, ApiResponse.Error(500, "AI填充服务暂时不可用"));
        }
    }

    /// <summary>
    /// 获取触发字段值
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="request">请求对象</param>
    /// <returns>触发字段值</returns>
    private static string? GetTriggerValue<T>(T request) where T : class
    {
        var dtoType = typeof(T);
        var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
        
        if (aiFormFillAttr == null || string.IsNullOrEmpty(aiFormFillAttr.TriggerField))
        {
            return "全局AI填充"; // 全局模式的默认值
        }

        var triggerProperty = dtoType.GetProperty(aiFormFillAttr.TriggerField);
        return triggerProperty?.GetValue(request)?.ToString();
    }
}
