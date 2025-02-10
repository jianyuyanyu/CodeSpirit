using CodeSpirit.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeSpirit.IdentityApi.Filters
{
    public class HttpResponseExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpResponseExceptionFilter> _logger;

        public HttpResponseExceptionFilter(ILogger<HttpResponseExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case AppServiceException appException:
                    HandleAppServiceException(context, appException);
                    break;

                case NullReferenceException nullRef:
                    HandleNotFound(context, nullRef, "空引用异常");
                    break;

                case ArgumentNullException argNull:
                    HandleBadRequest(context, "信息不存在！", argNull);
                    break;

                case BadHttpRequestException badHttp:
                    HandleBadRequest(context, badHttp.Message, badHttp);
                    break;

                case DBConcurrencyException dbConcurrency:
                    HandleConflict(context, dbConcurrency, "数据并发冲突");
                    break;

                case NotImplementedException notImplemented:
                    HandleNotImplemented(context, notImplemented);
                    break;

                case DbUpdateException dbUpdate:
                    HandleDbUpdateException(context, dbUpdate);
                    break;

                //case DaprException daprException:
                //    HandleDaprException(context, daprException);
                //    break;

                //case DaprApiException daprApiException:
                //    HandleDaprApiException(context, daprApiException);
                //    break;

                case UnauthorizedAccessException unauthorized:
                    HandleForbidden(context, unauthorized);
                    break;

                case TimeoutException timeout:
                    HandleGatewayTimeout(context, timeout);
                    break;

                case OperationCanceledException operationCanceled:
                    HandleOperationCanceled(context, operationCanceled);
                    break;

                case InvalidOperationException invalidOperation:
                    HandleConflict(context, invalidOperation, "无效操作");
                    break;

                case FileNotFoundException fileNotFound:
                    HandleNotFound(context, fileNotFound, "文件未找到");
                    break;

                case FormatException format:
                    HandleBadRequest(context, "格式错误", format);
                    break;

                case KeyNotFoundException keyNotFound:
                    HandleNotFound(context, keyNotFound, "键值未找到");
                    break;

                //case DbUpdateConcurrencyException dbUpdateConcurrency:
                //    HandleConflict(context, "数据库并发冲突", dbUpdateConcurrency);
                //    break;

                default:
                    SetServerErrorByException(context);
                    break;
            }

            context.ExceptionHandled = true;
        }

        private void HandleAppServiceException(ExceptionContext context, AppServiceException appException)
        {
            switch (appException.Code)
            {
                case 404:
                    context.Result = new NotFoundResult();
                    break;
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
            if (dbUpdateException.InnerException != null)
            {
                SetServerErrorByException(context, dbUpdateException.InnerException);
            }
            else
            {
                SetServerErrorByException(context);
            }
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

        private void SetServerErrorByException(ExceptionContext context, Exception exception = null)
        {
            Exception ex = exception ?? context.Exception;
            context.Result = new ObjectResult(ApiResponse<object>.Error(
                StatusCodes.Status500InternalServerError,
                "服务器繁忙，请稍后再试！"))
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };

            _logger.LogError(ex, ex?.Message);

            // 记录内部异常（如果有）
            if (ex?.InnerException != null)
            {
                _logger.LogError(ex.InnerException, ex.InnerException.Message);
            }
        }
    }
}
