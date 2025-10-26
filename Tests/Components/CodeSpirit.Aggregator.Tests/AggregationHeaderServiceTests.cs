using CodeSpirit.Aggregator.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace CodeSpirit.Aggregator.Tests.Services
{
    /// <summary>
    /// AggregationHeaderService的单元测试类
    /// 主要测试聚合头部生成的各种场景，包括：
    /// 1. 基本聚合字段的处理（静态替换）
    /// 2. 动态数据源替换
    /// 3. 动态数据补充
    /// 4. 嵌套属性的处理
    /// 5. 特殊字符处理
    /// 6. DTO场景处理
    /// </summary>
    public class AggregationHeaderServiceTests
    {
        private readonly Mock<ILogger<AggregationHeaderService>> _loggerMock;
        private readonly AggregationHeaderService _service;

        public AggregationHeaderServiceTests()
        {
            _loggerMock = new Mock<ILogger<AggregationHeaderService>>();
            _service = new AggregationHeaderService(_loggerMock.Object);
        }

        /// <summary>
        /// 测试目的：验证静态替换模板的聚合头部生成
        /// 验证点：
        /// 1. 静态模板替换格式是否正确
        /// 2. 多个字段的组合是否正确
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithStaticTemplate_ReturnsCorrectHeader()
        {
            // Arrange
            var modelType = typeof(StaticTemplateModel);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);

            // Assert
            Assert.Contains("createdBy#User-{value}", result);
            Assert.Contains("avatarUrl#https://example.com/avatar/{value}", result);
        }

        /// <summary>
        /// 测试目的：验证动态数据源替换的聚合头部生成
        /// 验证点：
        /// 1. 数据源路径格式是否正确
        /// 2. 响应字段提取是否正确
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithDynamicSource_ReturnsCorrectHeader()
        {
            // Arrange
            var modelType = typeof(DynamicSourceModel);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);

            // Assert
            Assert.Contains("updatedBy=/user/{value}.name", result);
            Assert.Contains("creator=/api/users/{value}.fullName", result);
        }

        /// <summary>
        /// 测试目的：验证动态数据补充的聚合头部生成
        /// 验证点：
        /// 1. 数据源和模板组合是否正确
        /// 2. 复杂模板格式是否正确
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithDynamicTemplate_ReturnsCorrectHeader()
        {
            // Arrange
            var modelType = typeof(DynamicTemplateModel);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);

            // Assert
            Assert.Contains("userName=/api/users/{value}.name#{value} ({field})", result);
            Assert.Contains("department=/org/{value}.info#{value}-{field}", result);
        }

        /// <summary>
        /// 测试目的：验证嵌套属性的聚合头部生成
        /// 验证点：
        /// 1. 嵌套路径是否正确
        /// 2. 多层嵌套处理是否正确
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithNestedProperties_ReturnsCorrectPath()
        {
            // Arrange
            var modelType = typeof(NestedModel);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);

            // Assert
            Assert.NotEmpty(result);
            var rules = result.Split(',').Select(r => r.Trim()).ToList();
            _loggerMock.Object.LogInformation($"Generated rules: {string.Join(", ", rules)}");

            // 验证规则格式
            Assert.Contains(rules, r => r.Contains("items.createdBy=/user/{value}.name"));
            Assert.Contains(rules, r => r.Contains("items.details.owner=/api/users/{value}.fullName"));
        }

        /// <summary>
        /// 测试目的：验证特殊字符和中文的处理
        /// 验证点：
        /// 1. Base64编码是否正确
        /// 2. 解码后的内容是否完整
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithSpecialCharacters_ReturnsEncodedString()
        {
            // Arrange
            var modelType = typeof(SpecialCharModel);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);

            // Assert
            Assert.True(IsBase64String(result));
            var decodedString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            _loggerMock.Object.LogInformation($"Decoded string: {decodedString}");
            
            Assert.Contains("中文字段=/api/users/{value}.名称", decodedString);
            Assert.Contains("特殊字段#User-{value}", decodedString);
        }

        /// <summary>
        /// 测试目的：验证DTO类型的聚合头部生成
        /// 验证点：
        /// 1. 带有DisplayName的字段处理
        /// 2. 复杂数据源路径处理
        /// </summary>
        [Fact]
        public void GenerateAggregationHeader_WithDtoModel_ReturnsCorrectHeader()
        {
            // Arrange
            var modelType = typeof(Task<ActionResult<ApiResponse<PageList<TestPublishHistoryDto>>>>);

            // Act
            var result = _service.GenerateAggregationHeader(modelType);
            _loggerMock.Object.LogInformation($"Generated header: {result}");

            // Assert
            Assert.True(IsBase64String(result), "结果应该是Base64编码的");
            var decodedString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            _loggerMock.Object.LogInformation($"Decoded header: {decodedString}");
            
            Assert.Contains("data.items.createdBy=/identity/api/identity/users/{value}.data.name#用户: {field}", decodedString);
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return false;
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 测试模型类
        private class StaticTemplateModel
        {
            [AggregateField(template: "User-{value}")]
            public string CreatedBy { get; set; }

            [AggregateField(template: "https://example.com/avatar/{value}")]
            public string AvatarUrl { get; set; }
        }

        private class DynamicSourceModel
        {
            [AggregateField(dataSource: "/user/{value}.name")]
            public string UpdatedBy { get; set; }

            [AggregateField(dataSource: "/api/users/{value}.fullName")]
            public string Creator { get; set; }
        }

        private class DynamicTemplateModel
        {
            [AggregateField(dataSource: "/api/users/{value}.name", template: "{value} ({field})")]
            public string UserName { get; set; }

            [AggregateField(dataSource: "/org/{value}.info", template: "{value}-{field}")]
            public string Department { get; set; }
        }

        private class NestedModel
        {
            [AggregateField]
            public NestedItems Items { get; set; }
        }

        private class NestedItems
        {
            [AggregateField(dataSource: "/user/{value}.name")]
            public string CreatedBy { get; set; }

            [AggregateField]
            public NestedDetails Details { get; set; }
        }

        private class NestedDetails
        {
            [AggregateField(dataSource: "/api/users/{value}.fullName")]
            public string Owner { get; set; }
        }

        private class SpecialCharModel
        {
            [AggregateField(dataSource: "/api/users/{value}.名称")]
            public string 中文字段 { get; set; }

            [AggregateField(template: "User-{value}")]
            public string 特殊字段 { get; set; }
        }

        private class TestPublishHistoryDto
        {
            [DisplayName("ID")]
            public int Id { get; set; }

            [DisplayName("发布人")]
            [AggregateField(dataSource: "/identity/api/identity/users/{value}.data.name", template: "用户: {field}")]
            public string CreatedBy { get; set; }
        }
    }
} 