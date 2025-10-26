using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.MappingProfiles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services;

/// <summary>
/// ExamPaper AutoMapper映射测试
/// </summary>
public class ExamPaperMappingTests : IDisposable
{
    private readonly IMapper _mapper;
    private readonly ServiceProvider _serviceProvider;

    public ExamPaperMappingTests()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(typeof(ExamPaperProfile));
        _serviceProvider = services.BuildServiceProvider();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();
    }

    #region ExamPaper to ExamPaperDto Mapping Tests

    [Fact]
    public void MapExamPaperToDto_WithScoreConversion_ShouldMapAllFields()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "测试试卷",
            Description = "测试描述",
            Type = ExamPaperType.Random,
            TotalScore = 120,
            PassScore = 72,
            Duration = 90,
            Status = ExamPaperStatus.Published,

            Version = 1,
            EnableScoreConversion = true,
            OriginalPassScore = 72,
            ConversionTargetFullScore = 100,
            ConversionDecimalPlaces = 1,
            ConversionRatio = 0.8333m,
            TenantId = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(examPaper.Id, dto.Id);
        Assert.Equal(examPaper.Name, dto.Name);
        Assert.Equal(examPaper.Description, dto.Description);
        Assert.Equal(examPaper.Type, dto.Type);
        Assert.Equal(examPaper.TotalScore, dto.TotalScore);
        Assert.Equal(examPaper.PassScore, dto.PassScore);
        Assert.Equal(examPaper.Duration, dto.Duration);
        Assert.Equal(examPaper.Status, dto.Status);

        Assert.Equal(examPaper.Version, dto.Version);
        
        // 成绩换算相关字段
        Assert.True(dto.EnableScoreConversion);
        Assert.Equal(72, dto.OriginalPassScore);
        Assert.Equal(100, dto.ConversionTargetFullScore);
        Assert.Equal(1, dto.ConversionDecimalPlaces);
        Assert.Equal(0.8333m, dto.ConversionRatio);
        Assert.NotEmpty(dto.ConversionDescription);
    }

    [Fact]
    public void MapExamPaperToDto_WithoutScoreConversion_ShouldMapBasicFields()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 2,
            Name = "普通试卷",
            Description = "不启用成绩换算",
            Type = ExamPaperType.Fixed,
            TotalScore = 100,
            PassScore = 60,
            Duration = 120,
            Status = ExamPaperStatus.Draft,

            Version = 1,
            EnableScoreConversion = false,
            TenantId = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(examPaper.Id, dto.Id);
        Assert.Equal(examPaper.Name, dto.Name);
        Assert.False(dto.EnableScoreConversion);
        Assert.Null(dto.OriginalPassScore);
        Assert.Null(dto.ConversionTargetFullScore);
        Assert.Equal(1, dto.ConversionDecimalPlaces); // 默认值
        Assert.Null(dto.ConversionRatio);
        Assert.Empty(dto.ConversionDescription);
    }

    #endregion

    #region ConversionDescription Generation Tests

    [Theory]
    [InlineData(150, 100, 0.6667, 90, 1, "150分制 → 100分制", "换算比例：0.6667", "及格分：90 → 60.0", "小数保留：1位")]
    [InlineData(120, 100, 0.8333, 72, 0, "120分制 → 100分制", "换算比例：0.8333", "及格分：72 → 60", "不保留小数")]
    [InlineData(100, 150, 1.5, 60, 2, "100分制 → 150分制", "换算比例：1.5", "及格分：60 → 90.00", "小数保留：2位")]
    public void ConversionDescriptionGeneration_ValidScenarios_ShouldContainExpectedElements(
        int originalFullScore,
        int targetFullScore,
        decimal ratio,
        int originalPassScore,
        int decimalPlaces,
        params string[] expectedContains)
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "换算测试",
            TotalScore = originalFullScore,
            PassScore = originalPassScore,
            EnableScoreConversion = true,
            OriginalPassScore = originalPassScore,
            ConversionTargetFullScore = targetFullScore,
            ConversionDecimalPlaces = decimalPlaces,
            ConversionRatio = ratio,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.ConversionDescription);
        
        foreach (var expectedText in expectedContains)
        {
            Assert.Contains(expectedText, dto.ConversionDescription);
        }
    }

    [Fact]
    public void ConversionDescriptionGeneration_MissingTargetFullScore_ShouldReturnEmptyDescription()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "缺少目标满分",
            TotalScore = 100,
            PassScore = 60,
            EnableScoreConversion = true,
            OriginalPassScore = 60,
            ConversionTargetFullScore = null, // 缺少目标满分
            ConversionDecimalPlaces = 1,
            ConversionRatio = 0.8m,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.Empty(dto.ConversionDescription);
    }

    [Fact]
    public void ConversionDescriptionGeneration_MissingConversionRatio_ShouldReturnEmptyDescription()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "缺少换算比例",
            TotalScore = 100,
            PassScore = 60,
            EnableScoreConversion = true,
            OriginalPassScore = 60,
            ConversionTargetFullScore = 120,
            ConversionDecimalPlaces = 1,
            ConversionRatio = null, // 缺少换算比例
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.Empty(dto.ConversionDescription);
    }

    [Fact]
    public void ConversionDescriptionGeneration_CompleteFormula_ShouldIncludeCorrectFormula()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "公式测试",
            TotalScore = 150,
            PassScore = 90,
            EnableScoreConversion = true,
            OriginalPassScore = 90,
            ConversionTargetFullScore = 100,
            ConversionDecimalPlaces = 1,
            ConversionRatio = 0.6667m,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.ConversionDescription);
        Assert.Contains("换算公式：换算后成绩 = 原始成绩 × 0.6667（保留1位小数）", dto.ConversionDescription);
    }

    [Theory]
    [InlineData(0, "不保留小数")]
    [InlineData(1, "保留1位小数")]
    [InlineData(2, "保留2位小数")]
    public void ConversionDescriptionGeneration_DifferentDecimalPlaces_ShouldShowCorrectText(
        int decimalPlaces, 
        string expectedDecimalText)
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "小数位数测试",
            TotalScore = 100,
            PassScore = 60,
            EnableScoreConversion = true,
            OriginalPassScore = 60,
            ConversionTargetFullScore = 120,
            ConversionDecimalPlaces = decimalPlaces,
            ConversionRatio = 1.2m,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.ConversionDescription);
        Assert.Contains(expectedDecimalText, dto.ConversionDescription);
    }

    #endregion

    #region GenerateRandomExamPaperDto to ExamPaper Mapping Tests

    [Fact]
    public void MapGenerateRandomExamPaperDtoToExamPaper_WithScoreConversion_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new GenerateRandomExamPaperDto
        {
            Name = "随机试卷",
            Description = "测试随机试卷映射",
            TotalScore = 120,
            PassScore = 72,
            Duration = 90,
            EnableScoreConversion = true,
            ConversionTargetFullScore = 100,
            ConversionDecimalPlaces = 1
        };

        // Act
        var examPaper = _mapper.Map<ExamPaper>(dto);

        // Assert
        Assert.NotNull(examPaper);
        Assert.Equal(dto.Name, examPaper.Name);
        Assert.Equal(dto.Description, examPaper.Description);
        Assert.Equal(dto.TotalScore, examPaper.TotalScore);
        Assert.Equal(dto.PassScore, examPaper.PassScore);
        Assert.Equal(dto.Duration, examPaper.Duration);
        Assert.True(examPaper.EnableScoreConversion);
        Assert.Equal(dto.ConversionTargetFullScore, examPaper.ConversionTargetFullScore);
        Assert.Equal(dto.ConversionDecimalPlaces, examPaper.ConversionDecimalPlaces);
    }

    [Fact]
    public void MapGenerateRandomExamPaperDtoToExamPaper_WithoutScoreConversion_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new GenerateRandomExamPaperDto
        {
            Name = "普通随机试卷",
            Description = "不启用成绩换算",
            TotalScore = 100,
            PassScore = 60,
            Duration = 120,
            EnableScoreConversion = false
        };

        // Act
        var examPaper = _mapper.Map<ExamPaper>(dto);

        // Assert
        Assert.NotNull(examPaper);
        Assert.Equal(dto.Name, examPaper.Name);
        Assert.False(examPaper.EnableScoreConversion);
        Assert.Null(examPaper.OriginalPassScore);
        Assert.Null(examPaper.ConversionTargetFullScore);
        Assert.Equal(1, examPaper.ConversionDecimalPlaces); // 默认值
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void ConversionDescriptionGeneration_EdgeCaseValues_ShouldHandleGracefully()
    {
        // Arrange - 边界值测试
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "边界值测试",
            TotalScore = 1000,
            PassScore = 600,
            EnableScoreConversion = true,
            OriginalPassScore = 600,
            ConversionTargetFullScore = 1,
            ConversionDecimalPlaces = 2,
            ConversionRatio = 0.001m,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.ConversionDescription);
        Assert.Contains("1000分制 → 1分制", dto.ConversionDescription);
        Assert.Contains("换算比例：0.001", dto.ConversionDescription);
        Assert.Contains("及格分：600 → 0.60", dto.ConversionDescription);
    }

    [Fact]
    public void ConversionDescriptionGeneration_LargeValues_ShouldHandleCorrectly()
    {
        // Arrange - 大数值测试
        var examPaper = new ExamPaper
        {
            Id = 1,
            Name = "大数值测试",
            TotalScore = 100,
            PassScore = 60,
            EnableScoreConversion = true,
            OriginalPassScore = 60,
            ConversionTargetFullScore = 1000,
            ConversionDecimalPlaces = 0,
            ConversionRatio = 10m,
            TenantId = "test-tenant"
        };

        // Act
        var dto = _mapper.Map<ExamPaperDto>(examPaper);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.ConversionDescription);
        Assert.Contains("100分制 → 1000分制", dto.ConversionDescription);
        Assert.Contains("换算比例：10", dto.ConversionDescription);
        Assert.Contains("及格分：60 → 600", dto.ConversionDescription);
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}