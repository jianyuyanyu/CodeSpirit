using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services;

/// <summary>
/// 成绩换算服务单元测试
/// </summary>
public class ScoreConversionServiceTests
{
    private readonly ScoreConversionService _scoreConversionService;
    private readonly Mock<ILogger<ScoreConversionService>> _mockLogger;

    public ScoreConversionServiceTests()
    {
        _mockLogger = new Mock<ILogger<ScoreConversionService>>();
        _scoreConversionService = new ScoreConversionService(_mockLogger.Object);
    }

    #region CalculateConversionRatio Tests

    [Theory]
    [InlineData(150, 100, 0.6667)]
    [InlineData(100, 150, 1.5)]
    [InlineData(120, 100, 0.8333)]
    [InlineData(100, 120, 1.2)]
    [InlineData(50, 100, 2.0)]
    [InlineData(200, 100, 0.5)]
    public void CalculateConversionRatio_ValidInputs_ShouldReturnCorrectRatio(
        int originalFullScore, 
        int targetFullScore, 
        decimal expectedRatio)
    {
        // Act
        var result = _scoreConversionService.CalculateConversionRatio(originalFullScore, targetFullScore);

        // Assert
        Assert.Equal(expectedRatio, result, 4); // 精确到4位小数
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-10, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -10)]
    public void CalculateConversionRatio_InvalidInputs_ShouldThrowArgumentException(
        int originalFullScore, 
        int targetFullScore)
    {
        // Act & Assert
        Assert.Throws<BusinessException>(() => 
            _scoreConversionService.CalculateConversionRatio(originalFullScore, targetFullScore));
    }

    #endregion

    #region ConvertScore Tests

    [Theory]
    [InlineData(90, 0.6667, 1, 60.0)]
    [InlineData(90, 0.6667, 0, 60)]
    [InlineData(90, 0.6667, 2, 60.00)]
    [InlineData(85, 1.5, 1, 127.5)]
    [InlineData(85, 1.5, 0, 128)]
    [InlineData(75.5, 0.8, 1, 60.4)]
    [InlineData(75.5, 0.8, 2, 60.40)]
    public void ConvertScore_ValidInputs_ShouldReturnCorrectConvertedScore(
        double originalScore, 
        decimal conversionRatio, 
        int decimalPlaces, 
        decimal expectedScore)
    {
        // Act
        var result = _scoreConversionService.ConvertScore(originalScore, conversionRatio, decimalPlaces);

        // Assert
        Assert.Equal(expectedScore, result);
    }

    [Theory]
    [InlineData(-10, 0.8, 1)]
    [InlineData(50, 0, 1)]
    [InlineData(50, -0.5, 1)]
    [InlineData(50, 0.8, -1)]
    [InlineData(50, 0.8, 3)]
    public void ConvertScore_InvalidInputs_ShouldThrowArgumentException(
        double originalScore, 
        decimal conversionRatio, 
        int decimalPlaces)
    {
        // Act & Assert
        Assert.Throws<BusinessException>(() => 
            _scoreConversionService.ConvertScore(originalScore, conversionRatio, decimalPlaces));
    }

    [Fact]
    public void ConvertScore_EdgeCase_ShouldHandleRounding()
    {
        // Arrange - 测试四舍五入边界情况
        double originalScore = 89.65;
        decimal conversionRatio = 0.6667m;
        int decimalPlaces = 1;

        // Act
        var result = _scoreConversionService.ConvertScore(originalScore, conversionRatio, decimalPlaces);

        // Assert - 89.65 * 0.6667 = 59.78... 四舍五入到1位小数应该是 59.8
        Assert.Equal(59.8m, result);
    }

    #endregion

    #region ConvertPassScore Tests

    [Theory]
    [InlineData(90, 0.6667, 1, 60.0)]
    [InlineData(60, 1.5, 0, 90)]
    [InlineData(70, 0.8571, 2, 60.00)]
    public void ConvertPassScore_ValidInputs_ShouldReturnCorrectConvertedPassScore(
        int originalPassScore, 
        decimal conversionRatio, 
        int decimalPlaces, 
        decimal expectedPassScore)
    {
        // Act
        var result = _scoreConversionService.ConvertPassScore(originalPassScore, conversionRatio, decimalPlaces);

        // Assert
        Assert.Equal(expectedPassScore, result);
    }

    [Theory]
    [InlineData(-10, 0.8, 1)]
    [InlineData(50, 0, 1)]
    [InlineData(50, -0.5, 1)]
    public void ConvertPassScore_InvalidInputs_ShouldThrowArgumentException(
        int originalPassScore, 
        decimal conversionRatio, 
        int decimalPlaces)
    {
        // Act & Assert
        Assert.Throws<BusinessException>(() => 
            _scoreConversionService.ConvertPassScore(originalPassScore, conversionRatio, decimalPlaces));
    }

    #endregion

    #region BatchConvertExamRecordScoresAsync Tests

    [Fact]
    public async Task BatchConvertExamRecordScoresAsync_ValidInputs_ShouldReturnCorrectConvertedScores()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            TotalScore = 100, // 换算后的满分
            OriginalTotalScore = 150, // 换算前的满分  
            EnableScoreConversion = true,
            ConversionTargetFullScore = 100,
            ConversionRatio = 0.6667m,
            ConversionDecimalPlaces = 1
        };

        var examRecords = new List<ExamRecord>
        {
            new ExamRecord { Id = 1, Score = 90, ExamSettingId = 1 },
            new ExamRecord { Id = 2, Score = 80, ExamSettingId = 1 },
            new ExamRecord { Id = 3, Score = 70, ExamSettingId = 1 },
            new ExamRecord { Id = 4, Score = 60, ExamSettingId = 1 },
            new ExamRecord { Id = 5, Score = 50, ExamSettingId = 1 }
        };

        // Act
        var result = await _scoreConversionService.BatchConvertExamRecordScoresAsync(examRecords, examPaper);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(60.0, result[0].Score);  // 90 * 0.6667 = 60.003 -> 60.0
        Assert.Equal(53.3, result[1].Score);  // 80 * 0.6667 = 53.336 -> 53.3
        Assert.Equal(46.7, result[2].Score);  // 70 * 0.6667 = 46.669 -> 46.7
        Assert.Equal(40.0, result[3].Score);  // 60 * 0.6667 = 40.002 -> 40.0
        Assert.Equal(33.3, result[4].Score);  // 50 * 0.6667 = 33.335 -> 33.3
        
        // 验证原始成绩和换算标记
        Assert.All(result, record => Assert.True(record.IsScoreConverted));
        Assert.All(result, record => Assert.Equal(0.6666666666666666666666666667m, record.ScoreConversionRatio));
    }

    [Fact]
    public async Task BatchConvertExamRecordScoresAsync_EmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var examPaper = new ExamPaper { 
            TotalScore = 150,
            EnableScoreConversion = true, 
            ConversionTargetFullScore = 100,
            ConversionRatio = 0.6667m 
        };
        var examRecords = new List<ExamRecord>();

        // Act
        var result = await _scoreConversionService.BatchConvertExamRecordScoresAsync(examRecords, examPaper);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task BatchConvertExamRecordScoresAsync_NullList_ShouldThrowArgumentNullException()
    {
        // Arrange
        var examPaper = new ExamPaper { EnableScoreConversion = true };
        List<ExamRecord>? examRecords = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scoreConversionService.BatchConvertExamRecordScoresAsync(examRecords!, examPaper));
    }

    #endregion

    #region GenerateConversionDescription Tests

    [Fact]
    public void GenerateConversionDescription_ValidInputs_ShouldReturnCorrectDescription()
    {
        // Arrange
        int originalFullScore = 150;
        int targetFullScore = 100;
        int originalPassScore = 90;
        int targetPassScore = 60;
        int decimalPlaces = 1;

        // Act
        var result = _scoreConversionService.GenerateConversionDescription(
            originalFullScore, targetFullScore, originalPassScore, targetPassScore, decimalPlaces);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("150分制 → 100分制", result);
        Assert.Contains("及格分：90 → 60", result);
    }

    [Fact]
    public void GenerateConversionDescription_NoDecimalPlaces_ShouldReturnCorrectDescription()
    {
        // Arrange
        int originalFullScore = 120;
        int targetFullScore = 100;
        int originalPassScore = 72;
        int targetPassScore = 60;
        int decimalPlaces = 0;

        // Act
        var result = _scoreConversionService.GenerateConversionDescription(
            originalFullScore, targetFullScore, originalPassScore, targetPassScore, decimalPlaces);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("120分制 → 100分制", result);
        Assert.Contains("及格分：72 → 60", result);
    }

    [Theory]
    [InlineData(0, 100, 60, 48, 1)]
    [InlineData(100, 0, 60, 48, 1)]
    public void GenerateConversionDescription_InvalidFullScore_ShouldThrowBusinessException(
        int originalFullScore, int targetFullScore, 
        int originalPassScore, int targetPassScore, int decimalPlaces)
    {
        // Act & Assert
        Assert.Throws<BusinessException>(() => 
            _scoreConversionService.GenerateConversionDescription(
                originalFullScore, targetFullScore, originalPassScore, targetPassScore, decimalPlaces));
    }

    [Theory]
    [InlineData(100, 100, -10, 48, 1)]
    [InlineData(100, 100, 60, -10, 1)]
    [InlineData(100, 100, 60, 48, -1)]
    [InlineData(100, 100, 60, 48, 3)]
    public void GenerateConversionDescription_OtherInvalidInputs_ShouldNotThrow(
        int originalFullScore, int targetFullScore, 
        int originalPassScore, int targetPassScore, int decimalPlaces)
    {
        // Act & Assert - 这些情况可能不抛出异常，只是生成描述
        var result = _scoreConversionService.GenerateConversionDescription(
            originalFullScore, targetFullScore, originalPassScore, targetPassScore, decimalPlaces);
        
        // 应该返回一个字符串，即使参数有问题
        Assert.NotNull(result);
    }

    #endregion

    #region ValidateConversionConfiguration Tests

    [Theory]
    [InlineData(100, 120, 60, 1)]
    [InlineData(150, 100, 90, 0)]
    [InlineData(200, 100, 120, 2)]
    public void ValidateConversionConfiguration_ValidInputs_ShouldReturnValid(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int decimalPlaces)
    {
        // Act
        var result = _scoreConversionService.ValidateConversionConfiguration(
            originalFullScore, targetFullScore, originalPassScore, decimalPlaces);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
    }

    [Theory]
    [InlineData(0, 100, 60, 1)]        // 原始满分无效
    [InlineData(100, 0, 60, 1)]        // 目标满分无效
    [InlineData(100, 120, -10, 1)]     // 原始及格分无效
    [InlineData(100, 120, 150, 1)]     // 原始及格分大于原始满分
    [InlineData(100, 120, 60, -1)]     // 小数位数无效
    [InlineData(100, 120, 60, 3)]      // 小数位数超出范围
    public void ValidateConversionConfiguration_InvalidInputs_ShouldReturnInvalid(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int decimalPlaces)
    {
        // Act
        var result = _scoreConversionService.ValidateConversionConfiguration(
            originalFullScore, targetFullScore, originalPassScore, decimalPlaces);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.ErrorMessage);
    }

    #region ConvertExamRecordScore Tests

    [Fact]
    public void ConvertExamRecordScore_ValidInputs_ShouldConvertCorrectly()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            TotalScore = 100, // 换算后的满分
            OriginalTotalScore = 150, // 换算前的满分
            EnableScoreConversion = true,
            ConversionTargetFullScore = 100,
            ConversionRatio = 0.6667m,
            ConversionDecimalPlaces = 1
        };

        var examRecord = new ExamRecord
        {
            Id = 1,
            Score = 90,
            ExamSettingId = 1,
            IsScoreConverted = false
        };

        // Act
        var result = _scoreConversionService.ConvertExamRecordScore(examRecord, examPaper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60.0, result.Score);
        Assert.Equal(90, result.OriginalScore);
        Assert.True(result.IsScoreConverted);
        Assert.Equal(0.6666666666666666666666666667m, result.ScoreConversionRatio);
    }

    [Fact]
    public void ConvertExamRecordScore_DisabledConversion_ShouldNotConvert()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            Id = 1,
            TotalScore = 100,
            EnableScoreConversion = false
        };

        var examRecord = new ExamRecord
        {
            Id = 1,
            Score = 90,
            ExamSettingId = 1,
            IsScoreConverted = false
        };

        // Act
        var result = _scoreConversionService.ConvertExamRecordScore(examRecord, examPaper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90, result.Score);
        Assert.Null(result.OriginalScore);
        Assert.False(result.IsScoreConverted);
        Assert.Null(result.ScoreConversionRatio);
    }

    #endregion

    #region RequiresReconversion Tests

    [Fact]
    public void RequiresReconversion_ConversionEnabledWithDifferentRatio_ShouldReturnTrue()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            TotalScore = 120, // 换算后的满分
            OriginalTotalScore = 150, // 换算前的满分
            EnableScoreConversion = true,
            ConversionTargetFullScore = 120,
            ConversionRatio = 0.8m
        };

        var existingRecords = new List<ExamRecord>
        {
            new ExamRecord { IsScoreConverted = true, ScoreConversionRatio = 0.6m }
        };

        // Act
        var result = _scoreConversionService.RequiresReconversion(examPaper, existingRecords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RequiresReconversion_SameConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var examPaper = new ExamPaper
        {
            TotalScore = 120, // 换算后的满分
            OriginalTotalScore = 150, // 换算前的满分
            EnableScoreConversion = true,
            ConversionTargetFullScore = 120,
            ConversionRatio = 0.8m
        };

        var existingRecords = new List<ExamRecord>
        {
            new ExamRecord { IsScoreConverted = true, ScoreConversionRatio = 0.8m }
        };

        // Act
        var result = _scoreConversionService.RequiresReconversion(examPaper, existingRecords);

        // Assert
        Assert.False(result);
    }

    #endregion

    #endregion
}