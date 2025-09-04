using CodeSpirit.AiFormFill.Services;
using CodeSpirit.AiFormFill.Models;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace CodeSpirit.AiFormFill.Middleware;

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

            // 解析请求数据，可能包含自定义提示词
            var (requestObject, customPrompt) = ParseRequestWithCustomPrompt(requestBody, endpointInfo.DtoType);
            if (requestObject == null)
            {
                await WriteErrorResponseAsync(context, 400, "无效的请求格式");
                return;
            }

            // 获取AI填充服务
            var aiFormFillService = context.RequestServices.GetRequiredService<IAiFormFillService>();

            // 执行AI填充
            var result = await ExecuteAiFillAsync(aiFormFillService, requestObject, endpointInfo, customPrompt);

            // 返回结果
            await WriteSuccessResponseAsync(context, result);
        }
        catch (BusinessException bex)
        {
            await WriteErrorResponseAsync(context, 400, bex.Message);
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
    /// 解析请求数据，提取自定义提示词
    /// </summary>
    /// <param name="requestBody">请求体</param>
    /// <param name="dtoType">DTO类型</param>
    /// <returns>解析后的请求对象和自定义提示词</returns>
    private (object? RequestObject, string CustomPrompt) ParseRequestWithCustomPrompt(string requestBody, Type dtoType)
    {
        try
        {
            // 首先尝试解析为包含自定义提示词的格式
            var jObject = JObject.Parse(requestBody);

            // 提取自定义提示词
            var customPrompt = jObject["customPrompt"]?.ToString() ?? string.Empty;

            // 移除自定义提示词字段，避免影响DTO反序列化
            jObject.Remove("customPrompt");
            jObject.Remove("_aiCustomPrompt"); // 移除前端字段

            // 清理空对象字段，避免反序列化到string类型时出错
            CleanEmptyObjectFields(jObject, dtoType);

            // 反序列化为目标DTO类型，使用宽松的设置
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            var requestObject = jObject.ToObject(dtoType, JsonSerializer.Create(settings));

            return (requestObject, customPrompt);
        }
        catch
        {
            // 如果解析失败，尝试直接反序列化
            try
            {
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                var requestObject = JsonConvert.DeserializeObject(requestBody, dtoType, settings);
                return (requestObject, string.Empty);
            }
            catch
            {
                return (null, string.Empty);
            }
        }
    }

    /// <summary>
    /// 清理空对象字段，避免反序列化错误
    /// </summary>
    /// <param name="jObject">JSON对象</param>
    /// <param name="dtoType">DTO类型</param>
    private void CleanEmptyObjectFields(JObject jObject, Type dtoType)
    {
        var properties = dtoType.GetProperties();
        var fieldsToRemove = new List<string>();

        foreach (var property in jObject.Properties().ToList())
        {
            var targetProperty = properties.FirstOrDefault(p =>
                string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));

            if (targetProperty != null)
            {
                // 如果目标属性是string类型，但JSON值是空对象，则移除该字段
                var isStringType = targetProperty.PropertyType == typeof(string) ||
                                   targetProperty.PropertyType.Name == "String";

                if (isStringType && property.Value.Type == JTokenType.Object && !property.Value.HasValues)
                {
                    fieldsToRemove.Add(property.Name);
                }
            }
        }

        // 移除有问题的字段
        foreach (var fieldName in fieldsToRemove)
        {
            jObject.Remove(fieldName);
        }
    }

    /// <summary>
    /// 执行AI填充
    /// </summary>
    /// <param name="aiFormFillService">AI填充服务</param>
    /// <param name="requestObject">请求对象</param>
    /// <param name="endpointInfo">端点信息</param>
    /// <param name="customPrompt">自定义提示词</param>
    /// <returns>填充结果</returns>
    private async Task<object> ExecuteAiFillAsync(
        IAiFormFillService aiFormFillService,
        object requestObject,
        AiFormFillEndpointInfo endpointInfo,
        string? customPrompt = null)
    {
        string triggerValue;

        // 检查是否为全局AI填充模式（TriggerField为空）
        if (string.IsNullOrEmpty(endpointInfo.TriggerField))
        {
            // 全局模式：优先使用自定义提示词，否则使用默认提示
            if (!string.IsNullOrEmpty(customPrompt?.Trim()))
            {
                triggerValue = customPrompt.Trim();
            }
            else
            {
                // 使用DTO类型名称作为默认触发值
                var dtoTypeName = endpointInfo.DtoType.Name;
                var displayName = dtoTypeName.EndsWith("Dto")
                    ? dtoTypeName.Substring(0, dtoTypeName.Length - 3)
                    : dtoTypeName;
                triggerValue = $"全局AI填充{displayName}";
            }
        }
        else
        {
            // 传统模式：从触发字段获取值
            var triggerProperty = endpointInfo.DtoType.GetProperty(endpointInfo.TriggerField);
            if (triggerProperty == null)
            {
                throw new InvalidOperationException($"未找到触发字段：{endpointInfo.TriggerField}");
            }

            triggerValue = triggerProperty.GetValue(requestObject)?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(triggerValue.Trim()))
            {
                var displayName = GetDisplayName(triggerProperty);
                throw new BusinessException($"请先输入{displayName}");
            }
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
    private string GetDisplayName(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayNameAttribute>();
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
