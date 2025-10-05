using System.ComponentModel;
using System.Reflection;
using CodeSpirit.Amis.Attributes.Columns;
using Xunit;

namespace CodeSpirit.Amis.Tests
{
    /// <summary>
    /// 测试属性值是否正确设置
    /// </summary>
    public class AttributeTest
    {
        public class TestDto
        {
            [DisplayName("HTTP状态码")]
            [AmisColumn(Type = "status", StatusMapping = StatusMapping.HttpStatusCode)]
            public int StatusCode { get; set; }
        }

        [Fact]
        public void AmisColumnAttribute_ShouldHaveCorrectValues()
        {
            // Arrange
            var property = typeof(TestDto).GetProperty(nameof(TestDto.StatusCode));
            
            // Act
            var columnAttr = (AmisColumnAttribute)Attribute.GetCustomAttribute(property, typeof(AmisColumnAttribute));
            
            // Assert
            Assert.NotNull(columnAttr);
            Assert.Equal("status", columnAttr.Type);
            Assert.Equal(StatusMapping.HttpStatusCode, columnAttr.StatusMapping);
            
            // 输出调试信息
            System.Console.WriteLine($"Type: '{columnAttr.Type}' (length: {columnAttr.Type?.Length ?? -1})");
            System.Console.WriteLine($"StatusMapping: {columnAttr.StatusMapping}");
        }

        [Fact]
        public void AuditLogDto_StatusCode_ShouldHaveCorrectAttribute()
        {
            // Arrange - 使用实际的 AuditLogDto
            var auditLogType = typeof(CodeSpirit.Audit.Services.Dtos.AuditLogDto);
            var property = auditLogType.GetProperty("StatusCode");
            
            // Act
            var columnAttr = (AmisColumnAttribute)Attribute.GetCustomAttribute(property, typeof(AmisColumnAttribute));
            
            // Assert
            Assert.NotNull(columnAttr);
            
            // 输出调试信息
            System.Console.WriteLine($"AuditLogDto.StatusCode - Type: '{columnAttr.Type}' (length: {columnAttr.Type?.Length ?? -1})");
            System.Console.WriteLine($"AuditLogDto.StatusCode - StatusMapping: {columnAttr.StatusMapping}");
            System.Console.WriteLine($"AuditLogDto.StatusCode - Type is null: {columnAttr.Type == null}");
            System.Console.WriteLine($"AuditLogDto.StatusCode - Type is empty: {string.IsNullOrEmpty(columnAttr.Type)}");
            
            // 验证值
            Assert.Equal("status", columnAttr.Type);
            Assert.Equal(StatusMapping.HttpStatusCode, columnAttr.StatusMapping);
        }
    }
}
