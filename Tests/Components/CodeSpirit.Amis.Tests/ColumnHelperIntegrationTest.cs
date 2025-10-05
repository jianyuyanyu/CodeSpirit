using System.ComponentModel;
using System.Reflection;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CodeSpirit.Amis.Tests
{
    /// <summary>
    /// ColumnHelper 集成测试
    /// 测试 CreateAmisColumn 方法是否正确处理状态映射
    /// </summary>
    public class ColumnHelperIntegrationTest
    {
        /// <summary>
        /// 测试用的DTO类
        /// </summary>
        public class TestStatusDto
        {
            [DisplayName("HTTP状态码")]
            [AmisColumn(Type = "status", StatusMapping = StatusMapping.HttpStatusCode)]
            public int StatusCode { get; set; }
        }

        [Fact]
        public void CreateAmisColumn_WithStatusMapping_ShouldGenerateMapAndLabelMap()
        {
            // Arrange
            var utilityHelper = new UtilityHelper();
            var property = typeof(TestStatusDto).GetProperty(nameof(TestStatusDto.StatusCode));
            
            // 使用反射调用私有方法 CreateAmisColumn
            var columnHelper = new ColumnHelper(null, utilityHelper, null, null);
            var createAmisColumnMethod = typeof(ColumnHelper).GetMethod("CreateAmisColumn", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = createAmisColumnMethod.Invoke(columnHelper, new object[] { property }) as JObject;

            // Assert
            Assert.NotNull(result);
            
            // 输出完整的 JSON 配置用于调试
            System.Console.WriteLine("Generated column config:");
            System.Console.WriteLine(result.ToString());
            
            // 检查属性是否有正确的特性
            var columnAttr = property.GetCustomAttribute<AmisColumnAttribute>();
            System.Console.WriteLine($"Property: {property.Name}, columnAttr: {columnAttr?.GetType().Name}");
            System.Console.WriteLine($"StatusMapping: {columnAttr?.StatusMapping}");
            
            Assert.Equal("status", result["type"]?.ToString());
            
            // 检查是否生成了 map 配置
            var map = result["map"] as JObject;
            Assert.NotNull(map);
            Assert.Equal("success", map["200"]?.ToString());
            Assert.Equal("warning", map["400"]?.ToString());
            Assert.Equal("danger", map["500"]?.ToString());

            // 检查是否生成了 labelMap 配置
            var labelMap = result["labelMap"] as JObject;
            Assert.NotNull(labelMap);
            Assert.Equal("OK", labelMap["200"]?.ToString());
            Assert.Equal("请求错误", labelMap["400"]?.ToString());
            Assert.Equal("服务器错误", labelMap["500"]?.ToString());

            // 输出完整的 JSON 配置用于调试
            System.Console.WriteLine("Generated column config:");
            System.Console.WriteLine(result.ToString());
        }
    }
}
