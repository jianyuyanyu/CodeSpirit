using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSpirit.Web.Extensions
{
    /// <summary>
    /// AI表单填充控制器扩展
    /// </summary>
    public static class AiFormFillControllerExtensions
    {
        /// <summary>
        /// 处理AI填充请求的通用方法
        /// </summary>
        /// <typeparam name="TDto">DTO类型</typeparam>
        /// <param name="controller">控制器实例</param>
        /// <param name="aiFormFillService">AI填充服务</param>
        /// <param name="request">请求对象</param>
        /// <returns>AI填充结果</returns>
        public static async Task<ActionResult<ApiResponse<TDto>>> HandleAiFillAsync<TDto>(
            this ControllerBase controller,
            IAiFormFillService aiFormFillService,
            TDto request) where TDto : class, new()
        {
            try
            {
                var dtoType = typeof(TDto);
                var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
                
                if (aiFormFillAttr == null)
                {
                    return controller.BadRequest(ApiResponse<TDto>.Error(400, "该表单不支持AI填充功能"));
                }

                // 获取触发字段的值
                var triggerProperty = dtoType.GetProperty(aiFormFillAttr.TriggerField);
                if (triggerProperty == null)
                {
                    return controller.BadRequest(ApiResponse<TDto>.Error(400, $"未找到触发字段：{aiFormFillAttr.TriggerField}"));
                }

                var triggerValue = triggerProperty.GetValue(request)?.ToString();
                if (string.IsNullOrEmpty(triggerValue?.Trim()))
                {
                    var displayName = GetDisplayName(triggerProperty);
                    return controller.BadRequest(ApiResponse<TDto>.Error(400, $"请先输入{displayName}"));
                }

                // 执行AI填充
                var result = await aiFormFillService.FillFormAsync(triggerValue, request);
                
                return controller.Ok(ApiResponse<TDto>.Success(result));
            }
            catch (BusinessException ex)
            {
                return controller.BadRequest(ApiResponse<TDto>.Error(400, ex.Message));
            }
            catch (Exception ex)
            {
                // 记录详细错误日志
                // TODO: 添加日志记录
                return controller.StatusCode(500, ApiResponse<TDto>.Error(500, "AI填充服务暂时不可用，请稍后重试"));
            }
        }

        /// <summary>
        /// 为控制器自动注册默认的AI填充端点
        /// </summary>
        /// <typeparam name="TDto">DTO类型</typeparam>
        /// <param name="controller">控制器实例</param>
        /// <param name="aiFormFillService">AI填充服务</param>
        /// <returns>AI填充端点的Action方法</returns>
        public static Func<TDto, Task<ActionResult<ApiResponse<TDto>>>> CreateDefaultAiFillAction<TDto>(
            this ControllerBase controller,
            IAiFormFillService aiFormFillService) where TDto : class, new()
        {
            return async (request) => await controller.HandleAiFillAsync(aiFormFillService, request);
        }

        /// <summary>
        /// 获取属性显示名称
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <returns>显示名称</returns>
        private static string GetDisplayName(PropertyInfo property)
        {
            var displayAttr = property.GetCustomAttribute<DisplayNameAttribute>();
            return displayAttr?.DisplayName ?? property.Name;
        }
    }



    /// <summary>
    /// AI表单填充服务接口
    /// </summary>
    public interface IAiFormFillService
    {
        /// <summary>
        /// 填充表单字段
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="triggerValue">触发值</param>
        /// <param name="existingData">现有数据</param>
        /// <returns>填充后的数据</returns>
        Task<T> FillFormAsync<T>(string triggerValue, T? existingData = null) where T : class, new();

        /// <summary>
        /// 验证DTO是否支持AI填充
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <returns>是否支持</returns>
        bool IsAiFillSupported<T>() where T : class;
    }
}
