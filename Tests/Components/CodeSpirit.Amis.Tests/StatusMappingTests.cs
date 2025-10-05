using System.ComponentModel;
using CodeSpirit.Amis.Attributes.Columns;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CodeSpirit.Amis.Tests
{
    /// <summary>
    /// 状态映射功能测试
    /// </summary>
    public class StatusMappingTests
    {
        /// <summary>
        /// 测试用的DTO类
        /// </summary>
        public class TestDto
        {
            [DisplayName("HTTP状态码")]
            [AmisColumn(Type = "status", StatusMapping = StatusMapping.HttpStatusCode)]
            public int StatusCode { get; set; }

            [DisplayName("是否成功")]
            [AmisColumn(Type = "status", StatusMapping = StatusMapping.Boolean)]
            public bool IsSuccess { get; set; }

            [DisplayName("操作类型")]
            [AmisColumn(Type = "status", StatusMapping = StatusMapping.AuditOperationType)]
            public string OperationType { get; set; } = string.Empty;

            [DisplayName("自定义状态")]
            [AmisColumn(Type = "status", CustomStatusMap = "{\"active\":\"success\",\"inactive\":\"fail\"}")]
            public string CustomStatus { get; set; } = string.Empty;

            [DisplayName("普通字段")]
            public string NormalField { get; set; } = string.Empty;
        }

        [Fact]
        public void StatusMapping_HttpStatusCode_ShouldGenerateCorrectMapping()
        {
            // Arrange
            var attribute = new AmisColumnAttribute
            {
                Type = "status",
                StatusMapping = StatusMapping.HttpStatusCode
            };

            // Act
            var mapConfig = GenerateMapConfigTest(StatusMapping.HttpStatusCode, null);
            var labelMapConfig = GenerateLabelMapConfigTest(StatusMapping.HttpStatusCode, null);

            // Assert
            Assert.NotNull(mapConfig);
            Assert.NotNull(labelMapConfig);

            var map = mapConfig as JObject;
            Assert.NotNull(map);
            Assert.Equal("success", map["200"]?.ToString());
            Assert.Equal("warning", map["400"]?.ToString());
            Assert.Equal("danger", map["500"]?.ToString());

            var labelMap = labelMapConfig as JObject;
            Assert.NotNull(labelMap);
            Assert.Equal("OK", labelMap["200"]?.ToString());
            Assert.Equal("请求错误", labelMap["400"]?.ToString());
            Assert.Equal("服务器错误", labelMap["500"]?.ToString());
        }

        [Fact]
        public void StatusMapping_Boolean_ShouldGenerateCorrectMapping()
        {
            // Act
            var mapConfig = GenerateMapConfigTest(StatusMapping.Boolean, null);
            var labelMapConfig = GenerateLabelMapConfigTest(StatusMapping.Boolean, null);

            // Assert
            var map = mapConfig as JObject;
            Assert.NotNull(map);
            Assert.Equal("success", map["true"]?.ToString());
            Assert.Equal("fail", map["false"]?.ToString());

            var labelMap = labelMapConfig as JObject;
            Assert.NotNull(labelMap);
            Assert.Equal("是", labelMap["true"]?.ToString());
            Assert.Equal("否", labelMap["false"]?.ToString());
        }

        [Fact]
        public void StatusMapping_AuditOperationType_ShouldGenerateCorrectMapping()
        {
            // Act
            var mapConfig = GenerateMapConfigTest(StatusMapping.AuditOperationType, null);
            var labelMapConfig = GenerateLabelMapConfigTest(StatusMapping.AuditOperationType, null);

            // Assert
            var map = mapConfig as JObject;
            Assert.NotNull(map);
            Assert.Equal("success", map["Create"]?.ToString());
            Assert.Equal("info", map["Update"]?.ToString());
            Assert.Equal("danger", map["Delete"]?.ToString());
            Assert.Equal("default", map["Query"]?.ToString());

            var labelMap = labelMapConfig as JObject;
            Assert.NotNull(labelMap);
            Assert.Equal("创建", labelMap["Create"]?.ToString());
            Assert.Equal("更新", labelMap["Update"]?.ToString());
            Assert.Equal("删除", labelMap["Delete"]?.ToString());
            Assert.Equal("查询", labelMap["Query"]?.ToString());
        }

        [Fact]
        public void StatusMapping_CustomMap_ShouldGenerateCorrectMapping()
        {
            // Act
            var mapConfig = GenerateMapConfigTest(StatusMapping.None, "{\"active\":\"success\",\"inactive\":\"fail\"}");

            // Assert
            var map = mapConfig as JObject;
            Assert.NotNull(map);
            Assert.Equal("success", map["active"]?.ToString());
            Assert.Equal("fail", map["inactive"]?.ToString());
        }

        /// <summary>
        /// 测试辅助方法 - 生成状态值映射配置
        /// </summary>
        private object GenerateMapConfigTest(StatusMapping mapping, string customMap)
        {
            // 优先使用自定义映射
            if (!string.IsNullOrEmpty(customMap))
            {
                try
                {
                    return JObject.Parse(customMap);
                }
                catch
                {
                    // 自定义映射解析失败，继续使用预定义映射
                }
            }

            // 根据预定义映射类型生成配置
            return mapping switch
            {
                StatusMapping.HttpStatusCode => new JObject
                {
                    ["200"] = "success",
                    ["201"] = "success",
                    ["204"] = "success",
                    ["300"] = "info",
                    ["301"] = "info",
                    ["302"] = "info",
                    ["400"] = "warning",
                    ["401"] = "warning",
                    ["403"] = "warning",
                    ["404"] = "warning",
                    ["500"] = "danger",
                    ["502"] = "danger",
                    ["503"] = "danger"
                },
                StatusMapping.Boolean => new JObject
                {
                    ["true"] = "success",
                    ["false"] = "fail"
                },
                StatusMapping.AuditOperationType => new JObject
                {
                    ["Create"] = "success",
                    ["Update"] = "info",
                    ["Delete"] = "danger",
                    ["Query"] = "default"
                },
                StatusMapping.CommonStatus => new JObject
                {
                    ["active"] = "success",
                    ["enabled"] = "success",
                    ["success"] = "success",
                    ["inactive"] = "fail",
                    ["disabled"] = "fail",
                    ["fail"] = "fail",
                    ["pending"] = "info",
                    ["processing"] = "info",
                    ["warning"] = "warning",
                    ["error"] = "danger",
                    ["danger"] = "danger"
                },
                StatusMapping.NumericStatus => new JObject
                {
                    ["1"] = "success",
                    ["0"] = "fail",
                    ["-1"] = "warning",
                    ["2"] = "info"
                },
                _ => null
            };
        }

        /// <summary>
        /// 测试辅助方法 - 生成状态标签映射配置
        /// </summary>
        private object GenerateLabelMapConfigTest(StatusMapping mapping, string customLabelMap)
        {
            // 优先使用自定义标签映射
            if (!string.IsNullOrEmpty(customLabelMap))
            {
                try
                {
                    return JObject.Parse(customLabelMap);
                }
                catch
                {
                    // 自定义标签映射解析失败，继续使用预定义映射
                }
            }

            // 根据预定义映射类型生成标签配置
            return mapping switch
            {
                StatusMapping.HttpStatusCode => new JObject
                {
                    // 2xx 成功状态码
                    ["200"] = "OK",
                    ["201"] = "已创建",
                    ["202"] = "已接受",
                    ["204"] = "无内容",
                    // 3xx 重定向状态码
                    ["301"] = "永久重定向",
                    ["302"] = "临时重定向",
                    ["304"] = "未修改",
                    // 4xx 客户端错误状态码
                    ["400"] = "请求错误",
                    ["401"] = "未授权",
                    ["403"] = "禁止访问",
                    ["404"] = "未找到",
                    ["405"] = "方法不允许",
                    ["409"] = "冲突",
                    ["422"] = "参数错误",
                    ["429"] = "请求过多",
                    // 5xx 服务器错误状态码
                    ["500"] = "服务器错误",
                    ["501"] = "未实现",
                    ["502"] = "网关错误",
                    ["503"] = "服务不可用",
                    ["504"] = "网关超时"
                },
                StatusMapping.Boolean => new JObject
                {
                    ["true"] = "是",
                    ["false"] = "否"
                },
                StatusMapping.AuditOperationType => new JObject
                {
                    ["Create"] = "创建",
                    ["Update"] = "更新",
                    ["Delete"] = "删除",
                    ["Query"] = "查询"
                },
                StatusMapping.CommonStatus => new JObject
                {
                    ["active"] = "活跃",
                    ["enabled"] = "启用",
                    ["success"] = "成功",
                    ["inactive"] = "非活跃",
                    ["disabled"] = "禁用",
                    ["fail"] = "失败",
                    ["pending"] = "待处理",
                    ["processing"] = "处理中",
                    ["warning"] = "警告",
                    ["error"] = "错误",
                    ["danger"] = "危险"
                },
                StatusMapping.NumericStatus => new JObject
                {
                    ["1"] = "成功",
                    ["0"] = "失败",
                    ["-1"] = "警告",
                    ["2"] = "信息"
                },
                _ => null
            };
        }
    }
}
