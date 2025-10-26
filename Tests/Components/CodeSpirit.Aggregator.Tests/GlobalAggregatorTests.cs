using CodeSpirit.Aggregator.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace CodeSpirit.Aggregator.Tests
{
    /// <summary>
    /// 全局聚合器功能测试
    /// </summary>
    public class GlobalAggregatorTests
    {
        private readonly IGlobalAggregatorConfigurationService _globalConfigService;
        private readonly AggregationHeaderService _aggregationHeaderService;
        private readonly Mock<ILogger<AggregationHeaderService>> _loggerMock;
        private readonly Mock<ILogger<GlobalAggregatorConfigurationService>> _globalLoggerMock;

        public GlobalAggregatorTests()
        {
            _loggerMock = new Mock<ILogger<AggregationHeaderService>>();
            _globalLoggerMock = new Mock<ILogger<GlobalAggregatorConfigurationService>>();
            _globalConfigService = new GlobalAggregatorConfigurationService(_globalLoggerMock.Object);
            _aggregationHeaderService = new AggregationHeaderService(_loggerMock.Object, _globalConfigService);
        }

        /// <summary>
        /// 测试全局规则注册
        /// </summary>
        public void TestGlobalRuleRegistration()
        {
            // Arrange
            var fieldName = "CreatedBy";
            var dataSource = "http://identity/api/identity/internal/users/{value}.data.name";
            var template = "{field}";

            // Act
            _globalConfigService.RegisterGlobalRule(fieldName, dataSource, template);

            // Assert
            var rule = _globalConfigService.GetGlobalRule(fieldName);
            if (rule == null)
                throw new Exception("全局规则注册失败");

            if (rule.FieldName != fieldName)
                throw new Exception($"字段名不匹配: 期望 {fieldName}, 实际 {rule.FieldName}");

            if (rule.DataSource != dataSource)
                throw new Exception($"数据源不匹配: 期望 {dataSource}, 实际 {rule.DataSource}");

            if (rule.Template != template)
                throw new Exception($"模板不匹配: 期望 {template}, 实际 {rule.Template}");
        }

        /// <summary>
        /// 测试全局规则应用
        /// </summary>
        public void TestGlobalRuleApplication()
        {
            // Arrange
            _globalConfigService.RegisterGlobalRule(
                "CreatedBy", 
                "http://identity/api/identity/internal/users/{value}.data.name", 
                "{field}");

            // Act
            var header = _aggregationHeaderService.GenerateAggregationHeader(typeof(TestDto));

            // Assert
            if (string.IsNullOrEmpty(header))
                throw new Exception("聚合头部生成失败");

            if (!header.Contains("createdBy="))
                throw new Exception("全局规则未应用到CreatedBy字段");
        }

        /// <summary>
        /// 测试特性优先级高于全局规则
        /// </summary>
        public void TestAttributePriorityOverGlobalRule()
        {
            // Arrange
            _globalConfigService.RegisterGlobalRule(
                "CustomField", 
                "/api/global/{value}.name", 
                "全局: {field}");

            // Act
            var header = _aggregationHeaderService.GenerateAggregationHeader(typeof(TestDtoWithAttribute));

            // Assert
            if (string.IsNullOrEmpty(header))
                throw new Exception("聚合头部生成失败");

            // 应该包含特性定义的规则，而不是全局规则
            if (!header.Contains("/api/custom/{value}.displayName"))
                throw new Exception("特性规则未正确应用");

            if (header.Contains("/api/global/{value}.name"))
                throw new Exception("全局规则错误地覆盖了特性规则");
        }

        /// <summary>
        /// 测试常用全局规则配置
        /// </summary>
        public void TestCommonGlobalRules()
        {
            // Act
            _globalConfigService.ConfigureCommonGlobalRules();

            // Assert
            var createdByRule = _globalConfigService.GetGlobalRule("CreatedBy");
            var updatedByRule = _globalConfigService.GetGlobalRule("UpdatedBy");
            var userIdRule = _globalConfigService.GetGlobalRule("UserId");

            if (createdByRule == null)
                throw new Exception("CreatedBy全局规则未配置");

            if (updatedByRule == null)
                throw new Exception("UpdatedBy全局规则未配置");

            if (userIdRule == null)
                throw new Exception("UserId全局规则未配置");

            // 验证数据源
            var expectedDataSource = "http://identity/api/identity/internal/users/{value}.data.name";
            if (createdByRule.DataSource != expectedDataSource)
                throw new Exception($"CreatedBy数据源不正确: {createdByRule.DataSource}");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            var tests = new GlobalAggregatorTests();

            try
            {
                tests.TestGlobalRuleRegistration();
                Console.WriteLine("✓ 全局规则注册测试通过");

                tests.TestGlobalRuleApplication();
                Console.WriteLine("✓ 全局规则应用测试通过");

                tests.TestAttributePriorityOverGlobalRule();
                Console.WriteLine("✓ 特性优先级测试通过");

                tests.TestCommonGlobalRules();
                Console.WriteLine("✓ 常用全局规则测试通过");

                Console.WriteLine("\n🎉 所有测试通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 测试用DTO - 包含CreatedBy字段但没有特性
    /// </summary>
    public class TestDto
    {
        [DisplayName("ID")]
        public string Id { get; set; }

        [DisplayName("标题")]
        public string Title { get; set; }

        [DisplayName("创建者")]
        public string CreatedBy { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 测试用DTO - 包含带特性的字段
    /// </summary>
    public class TestDtoWithAttribute
    {
        [DisplayName("ID")]
        public string Id { get; set; }

        [DisplayName("自定义字段")]
        [CodeSpirit.Core.Attributes.AggregateField(
            dataSource: "/api/custom/{value}.displayName", 
            template: "自定义: {field}")]
        public string CustomField { get; set; }
    }
}
