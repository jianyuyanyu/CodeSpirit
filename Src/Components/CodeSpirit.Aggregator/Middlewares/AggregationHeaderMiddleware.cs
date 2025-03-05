using CodeSpirit.Aggregator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using CodeSpirit.Core.Extensions;

namespace CodeSpirit.Aggregator.Middlewares
{
    public class AggregationHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAggregationHeaderService _headerService;
        private readonly ILogger<AggregationHeaderMiddleware> _logger;

        public AggregationHeaderMiddleware(
            RequestDelegate next,
            IAggregationHeaderService headerService,
            ILogger<AggregationHeaderMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _headerService = headerService ?? throw new ArgumentNullException(nameof(headerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 保存原始响应流
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                // 获取响应内容
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                // 尝试解析响应类型
                var responseType = GetResponseType(context);
                if (responseType != null)
                {
                    // 生成聚合规则头信息
                    var aggregationHeader = _headerService.GenerateAggregationHeader(responseType);
                    if (!string.IsNullOrEmpty(aggregationHeader))
                    {
                        context.Response.Headers["X-Aggregate-Keys"] = aggregationHeader;
                        _logger.LogInformation("添加聚合规则头信息: {Header}", aggregationHeader);
                    }
                }

                // 将响应内容写回原始流
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private Type GetResponseType(HttpContext context)
        {
            // 从路由数据中获取控制器和动作信息
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
                return null;

            var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerActionDescriptor == null)
                return null;

            // 获取返回类型
            var returnType = controllerActionDescriptor.MethodInfo.ReturnType;
            
            // 提取实际的响应类型
            if (returnType.IsGenericType)
            {
                var genericDef = returnType.GetGenericTypeDefinition();
                if (genericDef == typeof(Task<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }
            }

            return returnType;
        }
    }
}