using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Authorization.Tests
{
    /// <summary>
    /// HttpMethodHelper 类的单元测试
    /// </summary>
    public class HttpMethodHelperTests
    {
        // 测试控制器和动作样例
        private class TestController : ControllerBase
        {
            [HttpGet]
            public void GetMethod() { }

            [HttpPost]
            public void PostMethod() { }

            [HttpPut]
            public void PutMethod() { }

            [HttpDelete]
            public void DeleteMethod() { }

            [HttpPatch]
            public void PatchMethod() { }

            // 没有 HTTP 方法特性的方法
            public void NoHttpMethodAttribute() { }
        }

        /// <summary>
        /// 测试当动作有 HttpGet 特性时，应返回 "GET"
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithHttpGetAttribute_ReturnsGET()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.GetMethod));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal("GET", result);
        }

        /// <summary>
        /// 测试当动作有 HttpPost 特性时，应返回 "POST"
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithHttpPostAttribute_ReturnsPOST()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PostMethod));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal("POST", result);
        }

        /// <summary>
        /// 测试当动作有 HttpPut 特性时，应返回 "PUT"
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithHttpPutAttribute_ReturnsPUT()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PutMethod));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal("PUT", result);
        }

        /// <summary>
        /// 测试当动作有 HttpDelete 特性时，应返回 "DELETE"
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithHttpDeleteAttribute_ReturnsDELETE()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.DeleteMethod));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal("DELETE", result);
        }

        /// <summary>
        /// 测试当动作有 HttpPatch 特性时，应返回 "PATCH"
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithHttpPatchAttribute_ReturnsPATCH()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PatchMethod));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal("PATCH", result);
        }

        /// <summary>
        /// 测试当动作没有 HTTP 方法特性时，应返回空字符串
        /// </summary>
        [Fact]
        public void GetRequestMethod_WithNoHttpMethodAttribute_ReturnsEmptyString()
        {
            // Arrange
            var methodInfo = typeof(TestController).GetMethod(nameof(TestController.NoHttpMethodAttribute));

            // Act
            var result = HttpMethodHelper.GetRequestMethod(methodInfo);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}