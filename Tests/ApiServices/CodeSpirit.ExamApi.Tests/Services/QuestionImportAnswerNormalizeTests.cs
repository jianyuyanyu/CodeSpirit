using System.Reflection;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.ExamApi.Tests.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services;

/// <summary>
/// 题目导入答案标准化逻辑单元测试（NormalizeAnswerForImport）
/// </summary>
public class QuestionImportAnswerNormalizeTests : ExamServiceTestBase
{
    private readonly QuestionService _questionService;
    private static readonly MethodInfo NormalizeAnswerForImportMethod = typeof(QuestionService)
        .GetMethod("NormalizeAnswerForImport", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public QuestionImportAnswerNormalizeTests()
    {
        var singleChoiceLogger = new Mock<ILogger<SingleChoiceQuestionParser>>();
        var trueFalseLogger = new Mock<ILogger<TrueFalseQuestionParser>>();
        var multipleChoiceLogger = new Mock<ILogger<MultipleChoiceQuestionParser>>();
        var questionTextParser = new QuestionTextParserV2(
            new Mock<ILogger<QuestionTextParserV2>>().Object,
            new SingleChoiceQuestionParser(singleChoiceLogger.Object),
            new TrueFalseQuestionParser(trueFalseLogger.Object),
            new MultipleChoiceQuestionParser(multipleChoiceLogger.Object));
        var mockSettingsService = new Mock<CodeSpirit.Settings.Services.Interfaces.ISettingsService>();
        var mockCurrentUser = new Mock<CodeSpirit.Core.ICurrentUser>();
        mockCurrentUser.Setup(x => x.TenantId).Returns("test-tenant");
        var mockDistributedCache = new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
        var mockValidationService = new Mock<IQuestionValidationService>();
        var mockDataScopeService = new Mock<IExamDataScopeService>();
        mockDataScopeService.Setup(s => s.CanViewAllExamDataAsync()).ReturnsAsync(true);

        _questionService = new QuestionService(
            QuestionRepository,
            CategoryRepository,
            VersionRepository,
            Mapper,
            MockQuestionServiceLogger.Object,
            questionTextParser,
            ServiceProvider.GetRequiredService<CodeSpirit.Core.IdGenerator.IIdGenerator>(),
            mockSettingsService.Object,
            mockCurrentUser.Object,
            null!, // LLMAssistant - 测试中不调用
            mockDistributedCache.Object,
            mockValidationService.Object,
            mockDataScopeService.Object);
    }

    /// <summary>
    /// 通过反射调用私有方法 NormalizeAnswerForImport
    /// </summary>
    private void InvokeNormalizeAnswerForImport(CreateQuestionDto dto)
    {
        NormalizeAnswerForImportMethod.Invoke(_questionService, [dto]);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_LetterA_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["选项A", "选项B", "选项C", "选项D"],
            CorrectAnswer = "A",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项A", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_LetterB_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["苹果", "香蕉", "橙子", "葡萄"],
            CorrectAnswer = "B",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("香蕉", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_MultipleLetters_TakesFirstOnly()
    {
        // Arrange - 单选题多字母时只取第一个
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["选项1", "选项2", "选项3", "选项4"],
            CorrectAnswer = "AB",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项1", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_MultipleChoice_LetterAB_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.MultipleChoice,
            Options = ["SQL注入", "XSS", "CSRF", "合法访问"],
            CorrectAnswer = "ABC",
            CategoryId = 1,
            DefaultScore = 2
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("SQL注入,XSS,CSRF", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_MultipleChoice_LetterWithComma_ConvertsCorrectly()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.MultipleChoice,
            Options = ["选项1", "选项2", "选项3", "选项4"],
            CorrectAnswer = "A,B",
            CategoryId = 1,
            DefaultScore = 2
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项1,选项2", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_Number1_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["第一项", "第二项", "第三项", "第四项"],
            CorrectAnswer = "1",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("第一项", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_Number3_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["A", "B", "C", "D"],
            CorrectAnswer = "3",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("C", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_MultipleChoice_Number12_ConvertsToOptionContent()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.MultipleChoice,
            Options = ["选项1", "选项2", "选项3", "选项4"],
            CorrectAnswer = "1,2",
            CategoryId = 1,
            DefaultScore = 2
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项1,选项2", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_SingleChoice_NumberMultiple_TakesFirstOnly()
    {
        // Arrange - 单选题多数时只取第一个
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["选项1", "选项2", "选项3", "选项4"],
            CorrectAnswer = "1,2",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项1", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_AnswerAlreadyOptionContent_NoChange()
    {
        // Arrange - 答案已是选项内容，应保持不变
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["苹果", "香蕉", "橙子", "葡萄"],
            CorrectAnswer = "香蕉",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("香蕉", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_MultipleChoice_AnswerAlreadyOptionContent_NoChange()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.MultipleChoice,
            Options = ["SQL注入", "XSS", "CSRF", "合法访问"],
            CorrectAnswer = "SQL注入,XSS,CSRF",
            CategoryId = 1,
            DefaultScore = 2
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("SQL注入,XSS,CSRF", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_TrueFalse_NoChange()
    {
        // Arrange - 判断题不处理
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.TrueFalse,
            Options = ["True", "False"],
            CorrectAnswer = "True",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("True", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_NullDto_NoThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => InvokeNormalizeAnswerForImport(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void NormalizeAnswerForImport_EmptyAnswer_NoChange()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["A", "B", "C", "D"],
            CorrectAnswer = "",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_EmptyOptions_NoChange()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = [],
            CorrectAnswer = "A",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert - 无法转换，保持原样
        Assert.Equal("A", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_LetterE_WithFourOptions_NoChange()
    {
        // Arrange - E 超出选项范围，无法转换
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.SingleChoice,
            Options = ["A", "B", "C", "D"],
            CorrectAnswer = "E",
            CategoryId = 1,
            DefaultScore = 1
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert - 无法映射，保持原样
        Assert.Equal("E", dto.CorrectAnswer);
    }

    [Fact]
    public void NormalizeAnswerForImport_ChineseCommaSeparator_ConvertsCorrectly()
    {
        // Arrange
        var dto = new CreateQuestionDto
        {
            Content = "测试题目",
            Type = QuestionType.MultipleChoice,
            Options = ["选项1", "选项2", "选项3", "选项4"],
            CorrectAnswer = "A、B",
            CategoryId = 1,
            DefaultScore = 2
        };

        // Act
        InvokeNormalizeAnswerForImport(dto);

        // Assert
        Assert.Equal("选项1,选项2", dto.CorrectAnswer);
    }
}
