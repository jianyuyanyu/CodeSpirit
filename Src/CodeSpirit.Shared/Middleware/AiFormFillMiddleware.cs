using CodeSpirit.Core;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace CodeSpirit.Shared.Middleware;

/// <summary>
/// AI表单填充中间件
/// 自动处理AI填充请求
/// </summary>
public class AiFormFillMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AiFormFillMiddleware> _logger;
    private readonly AiFormFillEndpointScanner _endpointScanner;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="endpointScanner">端点扫描器</param>
    public AiFormFillMiddleware(
        RequestDelegate next,
        ILogger<AiFormFillMiddleware> logger,
        AiFormFillEndpointScanner endpointScanner)
    {
        _next = next;
        _logger = logger;
        _endpointScanner = endpointScanner;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>异步任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只处理POST请求
        if (context.Request.Method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            await _next(context);
            return;
        }

        // 查找匹配的AI填充端点
        var endpointInfo = _endpointScanner.FindEndpointByRoute(path);
        if (endpointInfo == null)
        {
            await _next(context);
            return;
        }

        try
        {
            _logger.LogInformation("处理AI填充请求: {Path} -> {DtoType}", path, endpointInfo.DtoType.Name);

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                await WriteErrorResponseAsync(context, 400, "请求体不能为空");
                return;
            }

            // 反序列化请求对象
            var requestObject = JsonConvert.DeserializeObject(requestBody, endpointInfo.DtoType);
            if (requestObject == null)
            {
                await WriteErrorResponseAsync(context, 400, "无效的请求格式");
                return;
            }

            // 获取AI填充服务
            var aiFormFillService = context.RequestServices.GetRequiredService<IAiFormFillService>();

            // 执行AI填充
            var result = await ExecuteAiFillAsync(aiFormFillService, requestObject, endpointInfo);

            // 返回结果
            await WriteSuccessResponseAsync(context, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理AI填充请求时发生错误: {Path}", path);
            await WriteErrorResponseAsync(context, 500, "AI填充服务暂时不可用，请稍后重试");
        }
    }

    /// <summary>
    /// 读取请求体
    /// </summary>
    /// <param name="request">HTTP请求</param>
    /// <returns>请求体内容</returns>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        
        request.Body.Position = 0;
        return body;
    }

    /// <summary>
    /// 执行AI填充
    /// </summary>
    /// <param name="aiFormFillService">AI填充服务</param>
    /// <param name="requestObject">请求对象</param>
    /// <param name="endpointInfo">端点信息</param>
    /// <returns>填充结果</returns>
    private async Task<object> ExecuteAiFillAsync(
        IAiFormFillService aiFormFillService, 
        object requestObject, 
        AiFormFillEndpointInfo endpointInfo)
    {
        // 获取触发字段的值
        var triggerProperty = endpointInfo.DtoType.GetProperty(endpointInfo.TriggerField);
        if (triggerProperty == null)
        {
            throw new InvalidOperationException($"未找到触发字段：{endpointInfo.TriggerField}");
        }

        var triggerValue = triggerProperty.GetValue(requestObject)?.ToString();
        if (string.IsNullOrEmpty(triggerValue?.Trim()))
        {
            var displayName = GetDisplayName(triggerProperty);
            throw new BusinessException($"请先输入{displayName}");
        }

        // 使用反射调用泛型方法
        var method = typeof(IAiFormFillService).GetMethod(nameof(IAiFormFillService.FillFormAsync))!;
        var genericMethod = method.MakeGenericMethod(endpointInfo.DtoType);
        
        var task = genericMethod.Invoke(aiFormFillService, new[] { triggerValue, requestObject });
        if (task is Task taskResult)
        {
            await taskResult;
            
            // 获取Task<T>的Result属性
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty != null)
            {
                return resultProperty.GetValue(task) ?? throw new InvalidOperationException("AI填充返回空结果");
            }
        }
        
        throw new InvalidOperationException("AI填充调用失败");
    }

    /// <summary>
    /// 获取属性显示名称
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>显示名称</returns>
    private string GetDisplayName(System.Reflection.PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        return displayAttr?.DisplayName ?? property.Name;
    }

    /// <summary>
    /// 写入成功响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="data">响应数据</param>
    /// <returns>异步任务</returns>
    private async Task WriteSuccessResponseAsync(HttpContext context, object data)
    {
        var response = ApiResponse<object>.Success(data, "AI优化完毕！");
        await WriteJsonResponseAsync(context, 200, response);
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="message">错误消息</param>
    /// <returns>异步任务</returns>
    private async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
    {
        var response = ApiResponse.Error(statusCode == 200 ? 1 : statusCode, message);
        await WriteJsonResponseAsync(context, statusCode, response);
    }

    /// <summary>
    /// 写入JSON响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="data">响应数据</param>
    /// <returns>异步任务</returns>
    private async Task WriteJsonResponseAsync(HttpContext context, int statusCode, object data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss"
        });

        await context.Response.WriteAsync(json, Encoding.UTF8);
    }
}
