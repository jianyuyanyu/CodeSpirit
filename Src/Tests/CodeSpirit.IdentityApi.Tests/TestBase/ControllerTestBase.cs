using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using CodeSpirit.Core;
using CodeSpirit.Shared.Filters;

namespace CodeSpirit.IdentityApi.Tests.TestBase
{
    /// <summary>
    /// 控制器测试基类
    /// </summary>
    public abstract class ControllerTestBase
    {
        /// <summary>
        /// 验证模型状态
        /// </summary>
        /// <typeparam name="TController">控制器类型</typeparam>
        /// <typeparam name="TModel">模型类型</typeparam>
        /// <param name="controller">控制器实例</param>
        /// <param name="model">模型实例</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>验证结果</returns>
        protected IActionResult ValidateModel<TController, TModel>(
            TController controller,
            TModel model,
            string propertyName,
            string errorMessage) where TController : ControllerBase
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError(propertyName, errorMessage);

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                modelState
            );

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>
                {
                    { "model", model }
                },
                controller
            );

            var validateModelAttribute = new ValidateModelAttribute();
            validateModelAttribute.OnActionExecuting(actionExecutingContext);

            return actionExecutingContext.Result;
        }

        /// <summary>
        /// 验证成功响应
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="actionResult">操作结果</param>
        /// <param name="expectedData">预期数据</param>
        /// <param name="expectedStatus">预期状态码</param>
        protected void AssertSuccessResponse<T>(ActionResult<ApiResponse<T>> actionResult, T expectedData, int expectedStatus = 0) where T : class
        {
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<T>>(objectResult.Value);
            Assert.Equal(expectedStatus, response.Status);
            Assert.Equal(expectedData, response.Data);
        }

        /// <summary>
        /// 验证错误响应
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="actionResult">操作结果</param>
        /// <param name="expectedStatus">预期状态码</param>
        /// <param name="expectedMessage">预期错误消息</param>
        protected void AssertErrorResponse<T>(ActionResult<ApiResponse<T>> actionResult, int expectedStatus, string expectedMessage) where T : class
        {
            var objectResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<T>>(objectResult.Value);
            Assert.Equal(expectedStatus, response.Status);
            Assert.Contains(expectedMessage, response.Msg);
        }

        /// <summary>
        /// 验证模型验证错误响应
        /// </summary>
        /// <param name="result">验证结果</param>
        /// <param name="expectedErrorMessage">预期错误消息</param>
        protected void AssertModelValidationError(IActionResult result, string expectedErrorMessage)
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ModelStateDictionary>>(badRequestResult.Value);
            Assert.Equal(400, response.Status);
            Assert.Contains(expectedErrorMessage, response.Msg);
        }

        /// <summary>
        /// 验证CreatedAtActionResult响应
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="result">操作结果</param>
        /// <param name="expectedData">预期数据</param>
        protected void AssertCreatedAtActionResult<T>(ActionResult<ApiResponse<T>> result, T expectedData) where T : class
        {
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<T>>(actionResult.Value);
            Assert.Equal(expectedData, response.Data);
        }

        protected void AssertPaginationResponse<T>(PageList<T> result, int expectedTotal, int expectedCount) where T : class
        {
            Assert.Equal(expectedTotal, result.Total);
            Assert.Equal(expectedCount, result.Items.Count);
        }

        protected void AssertUnauthorizedResponse<T>(ActionResult<ApiResponse<T>> result) where T : class
        {
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        /// <summary>
        /// 验证基础成功响应（不带数据）
        /// </summary>
        /// <param name="actionResult">操作结果</param>
        /// <param name="expectedStatus">预期状态码</param>
        protected void AssertBasicSuccessResponse(ActionResult<ApiResponse> actionResult, int expectedStatus = 0)
        {
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.Equal(expectedStatus, response.Status);
        }
    }
} 