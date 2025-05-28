using CodeSpirit.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace CodeSpirit.Shared.Filters
{
    /// <summary>
    /// 统一异常处理过滤器
    /// 提供标准化的异常处理和错误响应格式
    /// </summary>
    public class HttpResponseExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpResponseExceptionFilter> _logger;
        private readonly IWebHostEnvironment _environment;

        public HttpResponseExceptionFilter(
            ILogger<HttpResponseExceptionFilter> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// 异常处理主方法
        /// </summary>
        /// <param name="context">异常上下文</param>
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var traceId = context.HttpContext.TraceIdentifier;
            
            // 记录异常详细信息
            LogException(exception, context.HttpContext, traceId);

            // 根据异常类型进行处理
            var errorResponse = exception switch
            {
                BusinessException businessException => CreateAmisErrorResponse(
                    StatusCodes.Status400BadRequest,
                    businessException.Message,
                    "BUSINESS_ERROR",
                    traceId),

                ValidationException validationException => CreateValidationErrorResponse(
                    validationException,
                    traceId),

                AppServiceException appException => CreateAmisErrorResponse(
                    appException.Code >= 1000 ? StatusCodes.Status500InternalServerError : appException.Code,
                    appException.Message,
                    "BUSINESS_ERROR",
                    traceId),

                ArgumentNullException => CreateAmisErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "请求参数不能为空",
                    "INVALID_ARGUMENT",
                    traceId),

                ArgumentException => CreateAmisErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "请求参数无效",
                    "INVALID_ARGUMENT",
                    traceId),

                BadHttpRequestException badHttp => CreateAmisErrorResponse(
                    StatusCodes.Status400BadRequest,
                    badHttp.Message,
                    "BAD_REQUEST",
                    traceId),

                UnauthorizedAccessException => CreateAmisErrorResponse(
                    StatusCodes.Status403Forbidden,
                    "访问被拒绝，权限不足",
                    "FORBIDDEN",
                    traceId),

                FileNotFoundException => CreateAmisErrorResponse(
                    StatusCodes.Status404NotFound,
                    "请求的资源未找到",
                    "NOT_FOUND",
                    traceId),

                KeyNotFoundException => CreateAmisErrorResponse(
                    StatusCodes.Status404NotFound,
                    "请求的数据未找到",
                    "NOT_FOUND",
                    traceId),

                NotImplementedException => CreateAmisErrorResponse(
                    StatusCodes.Status501NotImplemented,
                    "功能尚未实现",
                    "NOT_IMPLEMENTED",
                    traceId),

                TimeoutException => CreateAmisErrorResponse(
                    StatusCodes.Status504GatewayTimeout,
                    "请求超时，请稍后重试",
                    "TIMEOUT",
                    traceId),

                OperationCanceledException => CreateAmisErrorResponse(
                    StatusCodes.Status499ClientClosedRequest,
                    "请求已取消",
                    "CANCELLED",
                    traceId),

                DBConcurrencyException => CreateAmisErrorResponse(
                    StatusCodes.Status409Conflict,
                    "数据并发冲突，请刷新后重试",
                    "CONCURRENCY_CONFLICT",
                    traceId),

                DbUpdateException dbUpdate => HandleDbUpdateException(dbUpdate, traceId),

                InvalidOperationException => CreateAmisErrorResponse(
                    StatusCodes.Status409Conflict,
                    "当前操作无效",
                    "INVALID_OPERATION",
                    traceId),

                FormatException => CreateAmisErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "数据格式错误",
                    "FORMAT_ERROR",
                    traceId),

                _ => CreateAmisErrorResponse(
                    StatusCodes.Status500InternalServerError,
                    _environment.IsDevelopment() ? exception.Message : "服务器内部错误",
                    "INTERNAL_ERROR",
                    traceId,
                    _environment.IsDevelopment() ? exception.StackTrace : null)
            };

            context.Result = errorResponse;
            context.ExceptionHandled = true;
        }

        private void HandleAppServiceException(ExceptionContext context, AppServiceException appException)
        {
            switch (appException.Code)
            {
                //case 404:
                //    context.Result = new NotFoundResult();
                //    break;
                case 401:
                    context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                    break;
                default:
                    context.Result = new ObjectResult(ApiResponse<object>.Error(
                        appException.Code >= 1000 ? StatusCodes.Status500InternalServerError : appException.Code,
                        appException.Message))
                    {
                        StatusCode = appException.Code >= 1000 ? StatusCodes.Status500InternalServerError : appException.Code,
                    };
                    _logger.LogError(context.Exception, "业务异常： Code:{Code} Msg:{Message}", appException.Code, appException.Message);
                    break;
            }
        }

        private void HandleNotFound(ExceptionContext context, Exception exception, string logMessage)
        {
            context.Result = new NotFoundResult();
            _logger.LogError(exception, $"{logMessage}：{exception.Message}");
        }

        private void HandleBadRequest(ExceptionContext context, string errorMessage, Exception exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status400BadRequest,
                errorMessage))
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            _logger.LogError(exception, errorMessage);
        }

        private void HandleConflict(ExceptionContext context, Exception exception, string logMessage)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status409Conflict,
                exception.Message))
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
            _logger.LogError(exception, $"{logMessage}：{exception.Message}");
        }

        private void HandleConflict(ExceptionContext context, string logMessage, Exception exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status409Conflict,
                "服务器繁忙，请重试。"))
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
            _logger.LogError(exception, $"{logMessage}：{exception.Message}");
        }

        private void HandleNotImplemented(ExceptionContext context, NotImplementedException exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status501NotImplemented,
                exception.Message))
            {
                StatusCode = StatusCodes.Status501NotImplemented,
            };
            _logger.LogError(exception, "接口未实现：{Message}", exception.Message);
        }

        private void HandleDbUpdateException(ExceptionContext context, DbUpdateException dbUpdateException)
        {
            _logger.LogError(dbUpdateException, dbUpdateException.Message);
            
            var traceId = context.HttpContext.TraceIdentifier;
            var errorResponse = HandleDbUpdateException(dbUpdateException, traceId);
            context.Result = errorResponse;
        }

        //private void HandleDaprException(ExceptionContext context, DaprException daprException)
        //{
        //    _logger.LogError(daprException, daprException.Message);
        //    if (daprException.InnerException != null)
        //    {
        //        SetServerErrorByException(context, daprException.InnerException);
        //    }
        //    else
        //    {
        //        SetServerErrorByException(context);
        //    }
        //}

        //private void HandleDaprApiException(ExceptionContext context, DaprApiException daprApiException)
        //{
        //    _logger.LogError(daprApiException, daprApiException.Message);
        //    SetServerErrorByException(context, daprApiException.InnerException);
        //}

        private void HandleForbidden(ExceptionContext context, UnauthorizedAccessException exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status403Forbidden,
                exception.Message))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            _logger.LogError(exception, "未授权访问：{Message}", exception.Message);
        }

        private void HandleGatewayTimeout(ExceptionContext context, TimeoutException exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status504GatewayTimeout,
                exception.Message))
            {
                StatusCode = StatusCodes.Status504GatewayTimeout,
            };
            _logger.LogError(exception, "操作超时：{Message}", exception.Message);
        }

        private void HandleOperationCanceled(ExceptionContext context, OperationCanceledException exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                499, // 自定义状态码
                "请求已取消"))
            {
                StatusCode = 499,
            };
            _logger.LogWarning(exception, "操作被取消：{Message}", exception.Message);
        }

        private void HandleConflict(ExceptionContext context, string logMessage, DbUpdateConcurrencyException exception)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status409Conflict,
                "服务器繁忙，请重试。"))
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
            _logger.LogError(exception, $"{logMessage}：{exception.Message}");
        }

        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="httpContext">HTTP上下文</param>
        /// <param name="traceId">跟踪ID</param>
        private void LogException(Exception exception, HttpContext httpContext, string traceId)
        {
            try
            {
                var logLevel = GetLogLevel(exception);
                var requestInfo = new
                {
                    TraceId = traceId,
                    Method = httpContext.Request.Method,
                    Path = httpContext.Request.Path.Value,
                    QueryString = httpContext.Request.QueryString.Value,
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    RemoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                };

                _logger.Log(logLevel, exception, 
                    "异常发生 - {ExceptionType}: {Message} | 请求信息: {@RequestInfo}",
                    exception.GetType().Name, exception.Message, requestInfo);
            }
            catch
            {
                // 如果日志记录失败，忽略错误，避免影响异常处理流程
                // 这确保即使日志系统出现问题，异常处理器仍能正常工作
            }
        }

        /// <summary>
        /// 根据异常类型确定日志级别
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <returns>日志级别</returns>
        private static LogLevel GetLogLevel(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException or FormatException => LogLevel.Warning,
                UnauthorizedAccessException or FileNotFoundException or KeyNotFoundException => LogLevel.Warning,
                BusinessException or ValidationException => LogLevel.Information,
                OperationCanceledException => LogLevel.Information,
                _ => LogLevel.Error
            };
        }

        /// <summary>
        /// 创建兼容 Amis 的错误响应
        /// </summary>
        /// <param name="statusCode">HTTP状态码</param>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="traceId">跟踪ID</param>
        /// <param name="details">错误详情</param>
        /// <returns>Amis兼容的错误响应</returns>
        private ObjectResult CreateAmisErrorResponse(
            int statusCode, 
            string message, 
            string errorCode, 
            string traceId,
            object? details = null)
        {
            // Amis API 标准响应格式
            var amisResponse = new
            {
                status = statusCode,
                msg = message,
                data = (object?)null,
                errors = details,
                traceId = traceId,
                timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return new ObjectResult(amisResponse)
            {
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// 创建验证错误响应
        /// </summary>
        /// <param name="validationException">验证异常</param>
        /// <param name="traceId">跟踪ID</param>
        /// <returns>验证错误响应</returns>
        private ObjectResult CreateValidationErrorResponse(ValidationException validationException, string traceId)
        {
            // 对于验证异常，使用 Amis 兼容格式
            return CreateAmisErrorResponse(
                StatusCodes.Status422UnprocessableEntity,
                validationException.Message,
                "VALIDATION_ERROR",
                traceId);
        }

        /// <summary>
        /// 处理数据库更新异常（新版本）
        /// </summary>
        /// <param name="dbUpdateException">数据库更新异常</param>
        /// <param name="traceId">跟踪ID</param>
        /// <returns>错误响应</returns>
        private ObjectResult HandleDbUpdateException(DbUpdateException dbUpdateException, string traceId)
        {
            var message = "数据操作失败";
            var errorCode = "DATABASE_ERROR";

            // 分析内部异常以提供更具体的错误信息
            if (dbUpdateException.InnerException != null)
            {
                var innerMessage = dbUpdateException.InnerException.Message.ToLower();
                if (innerMessage.Contains("unique") || innerMessage.Contains("duplicate"))
                {
                    message = "数据已存在，不能重复添加";
                    errorCode = "DUPLICATE_DATA";
                }
                else if (innerMessage.Contains("foreign key") || innerMessage.Contains("reference"))
                {
                    message = "数据关联约束冲突";
                    errorCode = "REFERENCE_CONSTRAINT";
                }
            }

            return CreateAmisErrorResponse(
                StatusCodes.Status409Conflict,
                message,
                errorCode,
                traceId,
                _environment.IsDevelopment() ? dbUpdateException.InnerException?.Message : null);
        }


    }
}
