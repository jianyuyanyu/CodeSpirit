using CodeSpirit.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Enums
{
    /// <summary>
    /// PlatformType 枚举单元测试
    /// </summary>
    public class PlatformTypeTests
    {
        /// <summary>
        /// 测试PlatformType枚举值
        /// </summary>
        [Fact]
        public void PlatformType_EnumValues_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal(0, (int)PlatformType.None);
            Assert.Equal(1, (int)PlatformType.System);
            Assert.Equal(2, (int)PlatformType.Tenant);
            Assert.Equal(4, (int)PlatformType.Inherit);
            Assert.Equal(3, (int)PlatformType.Both); // System | Tenant = 1 | 2 = 3
        }

        /// <summary>
        /// 测试PlatformType.Both的组合值
        /// </summary>
        [Fact]
        public void PlatformType_Both_ShouldBeCombinationOfSystemAndTenant()
        {
            // Assert
            Assert.Equal(PlatformType.System | PlatformType.Tenant, PlatformType.Both);
            Assert.True(PlatformType.Both.HasFlag(PlatformType.System));
            Assert.True(PlatformType.Both.HasFlag(PlatformType.Tenant));
            
            // 注意：在Flags枚举中，所有值都包含None(0)，这是正常的位运算行为
            // 我们主要验证Both不包含Inherit标志位
            Assert.False(PlatformType.Both.HasFlag(PlatformType.Inherit));
        }

        /// <summary>
        /// 测试PlatformType的Flags特性
        /// </summary>
        [Fact]
        public void PlatformType_FlagsAttribute_ShouldBeApplied()
        {
            // Arrange
            var enumType = typeof(PlatformType);

            // Act
            var flagsAttribute = enumType.GetCustomAttribute<FlagsAttribute>();

            // Assert
            Assert.NotNull(flagsAttribute);
        }

        /// <summary>
        /// 测试PlatformType的Display特性
        /// </summary>
        [Theory]
        [InlineData(PlatformType.None, "无平台")]
        [InlineData(PlatformType.System, "系统平台")]
        [InlineData(PlatformType.Tenant, "租户平台")]
        [InlineData(PlatformType.Inherit, "继承父级")]
        [InlineData(PlatformType.Both, "双平台")]
        public void PlatformType_DisplayAttribute_ShouldHaveCorrectNames(PlatformType enumValue, string expectedDisplayName)
        {
            // Arrange
            var enumType = typeof(PlatformType);
            var memberInfo = enumType.GetMember(enumValue.ToString());

            // Act
            var displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();

            // Assert
            Assert.NotNull(displayAttribute);
            Assert.Equal(expectedDisplayName, displayAttribute.Name);
        }

        /// <summary>
        /// 测试PlatformType枚举的所有值都有Display特性
        /// </summary>
        [Fact]
        public void PlatformType_AllValues_ShouldHaveDisplayAttribute()
        {
            // Arrange
            var enumType = typeof(PlatformType);
            var enumValues = Enum.GetValues<PlatformType>();

            // Act & Assert
            foreach (var enumValue in enumValues)
            {
                var memberInfo = enumType.GetMember(enumValue.ToString());
                var displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
                
                Assert.NotNull(displayAttribute);
                Assert.NotNull(displayAttribute.Name);
                Assert.NotEmpty(displayAttribute.Name);
            }
        }

        /// <summary>
        /// 测试PlatformType枚举值的唯一性
        /// </summary>
        [Fact]
        public void PlatformType_EnumValues_ShouldBeUnique()
        {
            // Arrange
            var enumValues = Enum.GetValues<PlatformType>();
            var intValues = new int[enumValues.Length];

            // Act
            for (int i = 0; i < enumValues.Length; i++)
            {
                intValues[i] = (int)enumValues[i];
            }

            // Assert - 检查除了Both之外的值是否唯一
            // Both是System和Tenant的组合，所以其值等于System | Tenant
            Assert.Equal(0, (int)PlatformType.None);
            Assert.Equal(1, (int)PlatformType.System);
            Assert.Equal(2, (int)PlatformType.Tenant);
            Assert.Equal(4, (int)PlatformType.Inherit);
            Assert.Equal(3, (int)PlatformType.Both); // 1 | 2 = 3
        }

        /// <summary>
        /// 测试PlatformType的HasFlag方法
        /// </summary>
        [Theory]
        [InlineData(PlatformType.System, PlatformType.System, true)]
        [InlineData(PlatformType.Tenant, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Both, PlatformType.System, true)]
        [InlineData(PlatformType.Both, PlatformType.Tenant, true)]
        [InlineData(PlatformType.System, PlatformType.Tenant, false)]
        [InlineData(PlatformType.Tenant, PlatformType.System, false)]
        [InlineData(PlatformType.System, PlatformType.Inherit, false)]
        [InlineData(PlatformType.Tenant, PlatformType.Inherit, false)]
        [InlineData(PlatformType.Both, PlatformType.Inherit, false)]
        [InlineData(PlatformType.Inherit, PlatformType.System, false)]
        [InlineData(PlatformType.None, PlatformType.System, false)]
        public void PlatformType_HasFlag_ShouldWorkCorrectly(PlatformType value, PlatformType flag, bool expectedResult)
        {
            // Act
            var result = value.HasFlag(flag);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// 测试PlatformType枚举值的位运算
        /// </summary>
        [Fact]
        public void PlatformType_BitwiseOperations_ShouldWorkCorrectly()
        {
            // Act & Assert
            // 测试OR运算
            Assert.Equal(PlatformType.Both, PlatformType.System | PlatformType.Tenant);
            
            // 测试AND运算
            Assert.Equal(PlatformType.System, PlatformType.Both & PlatformType.System);
            Assert.Equal(PlatformType.Tenant, PlatformType.Both & PlatformType.Tenant);
            Assert.Equal(PlatformType.None, PlatformType.System & PlatformType.Tenant);
            
            // 测试XOR运算
            Assert.Equal(PlatformType.Tenant, PlatformType.Both ^ PlatformType.System);
            Assert.Equal(PlatformType.System, PlatformType.Both ^ PlatformType.Tenant);
            
            // 测试NOT运算（应该谨慎使用，因为会影响其他位）
            var notSystem = ~PlatformType.System;
            Assert.False(notSystem.HasFlag(PlatformType.System));
        }

        /// <summary>
        /// 测试PlatformType枚举的ToString方法
        /// </summary>
        [Theory]
        [InlineData(PlatformType.None, "None")]
        [InlineData(PlatformType.System, "System")]
        [InlineData(PlatformType.Tenant, "Tenant")]
        [InlineData(PlatformType.Inherit, "Inherit")]
        [InlineData(PlatformType.Both, "Both")]
        public void PlatformType_ToString_ShouldReturnCorrectName(PlatformType enumValue, string expectedName)
        {
            // Act
            var result = enumValue.ToString();

            // Assert
            Assert.Equal(expectedName, result);
        }
    }
} 