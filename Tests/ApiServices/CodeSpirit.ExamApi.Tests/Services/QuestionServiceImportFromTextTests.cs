using System.Linq;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.ExamApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services;

/// <summary>
/// <see cref="QuestionService.ImportFromTextAsync"/> 与文本解析联调的单元测试（烹饪题库等真实格式）。
/// </summary>
public class QuestionServiceImportFromTextTests : ExamServiceTestBase
{
    private const string CookingImportSample = """
        27、关于生抽和老抽的区别，说法正确的有（ABD）
        A、生抽主打提鲜增味
        B、老抽主打上色提亮
        C、生抽颜色浓稠深沉
        D、老抽质地黏稠、颜色深
        【难度】中等
        【解析】生抽质地稀薄、颜色浅，用于凉拌、炒菜提鲜；老抽添加焦糖色，质地黏稠、颜色深，用于红烧、焖煮上色，二者均有咸味。
        【标签】烹饪调味、酱油使用

        1、烹饪中最基础的传热介质之一，常用于焯水、煮制菜品的是（A）
        A、水
        B、油
        C、蒸汽
        D、盐
        【难度】简单
        【解析】水是烹饪中最常用、最基础的传热介质，成本低廉、操作简便，主要用于焯水、煮、炖、焖等烹调方法，是新手入门必须掌握的基础传热知识。
        【标签】烹调工艺、传热介质
        """;

    private const string HalBrineAbcdSample = """
        45、熬制和养护老卤汤的关键要点有（ABCD）
        A、香料配比精准，避免过苦过淡
        B、每次卤制后过滤杂质
        C、严禁加入生水，防止卤汤变质
        D、冷却后密封冷藏保存
        【难度】困难
        【解析】老卤汤是卤菜灵魂，需精准控香料、勤过滤、防水、低温保存，养护得当可反复使用，滋味越卤越醇厚。
        【标签】烹调工艺、卤制技法
        """;

    private readonly QuestionService _questionService;

    /// <summary>
    /// 初始化导入测试用的 <see cref="QuestionService"/>（使用真实 <see cref="QuestionValidationService"/>）。
    /// </summary>
    public QuestionServiceImportFromTextTests()
    {
        MockCurrentUser.Setup(x => x.TenantId).Returns("test-tenant");

        var singleChoiceLogger = new Mock<ILogger<SingleChoiceQuestionParser>>();
        var trueFalseLogger = new Mock<ILogger<TrueFalseQuestionParser>>();
        var multipleChoiceLogger = new Mock<ILogger<MultipleChoiceQuestionParser>>();
        var questionTextParser = new QuestionTextParserV2(
            new Mock<ILogger<QuestionTextParserV2>>().Object,
            new SingleChoiceQuestionParser(singleChoiceLogger.Object),
            new TrueFalseQuestionParser(trueFalseLogger.Object),
            new MultipleChoiceQuestionParser(multipleChoiceLogger.Object));

        var mockSettingsService = new Mock<CodeSpirit.Settings.Services.Interfaces.ISettingsService>();
        mockSettingsService
            .Setup(s => s.GetTenantSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var mockDataScopeService = new Mock<IExamDataScopeService>();
        mockDataScopeService.Setup(s => s.CanViewAllExamDataAsync()).ReturnsAsync(true);

        var validationService = new QuestionValidationService(NullLogger<QuestionValidationService>.Instance);

        _questionService = new QuestionService(
            QuestionRepository,
            CategoryRepository,
            VersionRepository,
            Mapper,
            MockQuestionServiceLogger.Object,
            questionTextParser,
            ServiceProvider.GetRequiredService<CodeSpirit.Core.IdGenerator.IIdGenerator>(),
            mockSettingsService.Object,
            MockCurrentUser.Object,
            null!,
            new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>().Object,
            validationService,
            mockDataScopeService.Object);

        SeedCategories(new QuestionCategory
        {
            Id = 1,
            Name = "导入测试分类",
            TenantId = "test-tenant"
        });
        DbContext.SaveChanges();
    }

    /// <summary>
    /// 烹饪调味示例文本应成功导入 2 题，且不应出现「答案使用字母序号格式」类验证失败。
    /// </summary>
    [Fact]
    public async Task ImportFromTextAsync_CookingSample_TwoQuestions_SucceedsWithoutLetterAnswerValidationError()
    {
        var input = new QuestionImportFromTextDto
        {
            CategoryId = 1,
            Text = CookingImportSample
        };

        var (successCount, failedItems) = await _questionService.ImportFromTextAsync(input);

        Assert.Equal(2, successCount);
        Assert.Empty(failedItems);

        var questions = DbContext.Set<Question>().Where(q => q.CategoryId == 1).ToList();
        Assert.Equal(2, questions.Count);

        var multi = questions.Single(q => q.Type == QuestionType.MultipleChoice);
        Assert.Contains("生抽主打提鲜增味", multi.CorrectAnswer);
        Assert.Contains("老抽主打上色提亮", multi.CorrectAnswer);
        Assert.Contains("老抽质地黏稠", multi.CorrectAnswer);
        Assert.DoesNotContain("生抽颜色浓稠深沉", multi.CorrectAnswer);
        Assert.Equal(QuestionDifficulty.Medium, multi.Difficulty);

        var single = questions.Single(q => q.Type == QuestionType.SingleChoice);
        Assert.Equal("水", single.CorrectAnswer);
        Assert.Equal(QuestionDifficulty.Easy, single.Difficulty);
    }

    /// <summary>
    /// 多选 ABCD 且选项正文含中文逗号「，」时应成功导入（不得以「，」拆碎单条选项）。
    /// </summary>
    [Fact]
    public async Task ImportFromTextAsync_HalBrineAbcd_AllFourOptionsWithChineseCommaInText_Succeeds()
    {
        var input = new QuestionImportFromTextDto
        {
            CategoryId = 1,
            Text = HalBrineAbcdSample
        };

        var (successCount, failedItems) = await _questionService.ImportFromTextAsync(input);

        Assert.Equal(1, successCount);
        Assert.Empty(failedItems);

        var q = await DbContext.Set<Question>().SingleAsync(x => x.CategoryId == 1);
        Assert.Equal(QuestionType.MultipleChoice, q.Type);
        Assert.Equal(4, q.Options.Count);
        var parts = q.CorrectAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(4, parts.Length);
        Assert.Equal(q.Options[0], parts[0]);
        Assert.Equal(q.Options[1], parts[1]);
        Assert.Equal(q.Options[2], parts[2]);
        Assert.Equal(q.Options[3], parts[3]);
        Assert.Equal(QuestionDifficulty.Hard, q.Difficulty);
    }
}
