using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Tests.TestBase;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services;

/// <summary>
/// 试卷服务标签规则单元测试
/// </summary>
public class ExamPaperServiceTagRuleTests : ExamServiceTestBase
{
    private readonly ExamPaperService _examPaperService;
    private readonly Mock<ILogger<ExamPaperService>> _mockLogger;
    private readonly Repository<ExamPaper> _examPaperRepository;
    private readonly Repository<ExamPaperQuestion> _examPaperQuestionRepository;
    private readonly Mock<IScoreConversionService> _mockScoreConversionService;
    private readonly Mock<IExamDataScopeService> _mockDataScopeService;

    public ExamPaperServiceTagRuleTests()
    {
        _mockLogger = new Mock<ILogger<ExamPaperService>>();
        _examPaperRepository = CreateRepository<ExamPaper>();
        _examPaperQuestionRepository = CreateRepository<ExamPaperQuestion>();
        _mockScoreConversionService = new Mock<IScoreConversionService>();
        _mockDataScopeService = new Mock<IExamDataScopeService>();
        _mockDataScopeService.Setup(s => s.CanViewAllExamDataAsync()).ReturnsAsync(true);

        _examPaperService = new ExamPaperService(
            _examPaperRepository,
            _examPaperQuestionRepository,
            QuestionRepository,
            VersionRepository,
            Mapper,
            _mockLogger.Object,
            _mockScoreConversionService.Object,
            _mockDataScopeService.Object
        );

        // 准备测试数据
        SeedTestData();
    }

    /// <summary>
    /// 辅助方法：检查题目是否包含指定标签（处理Unicode转义格式）
    /// </summary>
    private static bool HasTag(Question question, string tagName)
    {
        if (string.IsNullOrWhiteSpace(question.Tags))
            return false;
            
        try
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(question.Tags);
            return tags != null && tags.Contains(tagName);
        }
        catch
        {
            return false;
        }
    }

    protected override void SeedTestData()
    {
        // 创建测试分类
        var category = new QuestionCategory
        {
            Id = 1,
            Name = "测试分类",
            TenantId = "tenant1"
        };
        SeedCategories(category);

        // 创建带不同标签的题目
        var questions = new List<Question>();
        
        // 单选题 - 标签A (10题)
        for (int i = 1; i <= 10; i++)
        {
            questions.Add(new Question
            {
                Id = i,
                Content = $"单选题{i} - 标签A",
                Type = QuestionType.SingleChoice,
                Difficulty = i <= 3 ? QuestionDifficulty.Easy :
                           i <= 7 ? QuestionDifficulty.Medium : QuestionDifficulty.Hard,
                Options = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "选项1",
                CategoryId = 1,
                Tags = System.Text.Json.JsonSerializer.Serialize(new List<string> { "标签A" }),
                DefaultScore = 5,
                TenantId = "tenant1"
            });
        }

        // 单选题 - 标签B (10题)
        for (int i = 11; i <= 20; i++)
        {
            questions.Add(new Question
            {
                Id = i,
                Content = $"单选题{i} - 标签B",
                Type = QuestionType.SingleChoice,
                Difficulty = i <= 13 ? QuestionDifficulty.Easy :
                           i <= 17 ? QuestionDifficulty.Medium : QuestionDifficulty.Hard,
                Options = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "选项1",
                CategoryId = 1,
                Tags = System.Text.Json.JsonSerializer.Serialize(new List<string> { "标签B" }),
                DefaultScore = 5,
                TenantId = "tenant1"
            });
        }

        // 单选题 - 无标签 (5题)
        for (int i = 21; i <= 25; i++)
        {
            questions.Add(new Question
            {
                Id = i,
                Content = $"单选题{i} - 无标签",
                Type = QuestionType.SingleChoice,
                Difficulty = QuestionDifficulty.Medium,
                Options = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "选项1",
                CategoryId = 1,
                Tags = null,
                DefaultScore = 5,
                TenantId = "tenant1"
            });
        }

        // 多选题 - 标签A (5题)
        for (int i = 26; i <= 30; i++)
        {
            questions.Add(new Question
            {
                Id = i,
                Content = $"多选题{i} - 标签A",
                Type = QuestionType.MultipleChoice,
                Difficulty = QuestionDifficulty.Medium,
                Options = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "选项1,选项2",
                CategoryId = 1,
                Tags = System.Text.Json.JsonSerializer.Serialize(new List<string> { "标签A" }),
                DefaultScore = 10,
                TenantId = "tenant1"
            });
        }

        // 多选题 - 标签B (5题)
        for (int i = 31; i <= 35; i++)
        {
            questions.Add(new Question
            {
                Id = i,
                Content = $"多选题{i} - 标签B",
                Type = QuestionType.MultipleChoice,
                Difficulty = QuestionDifficulty.Medium,
                Options = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "选项1,选项2",
                CategoryId = 1,
                Tags = System.Text.Json.JsonSerializer.Serialize(new List<string> { "标签B" }),
                DefaultScore = 10,
                TenantId = "tenant1"
            });
        }

        SeedQuestions(questions.ToArray());

        // 为每个题目创建版本
        var versions = new List<QuestionVersion>();
        foreach (var question in questions)
        {
            versions.Add(new QuestionVersion
            {
                Id = question.Id + 1000,
                QuestionId = question.Id,
                Content = question.Content,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Analysis = "解析",
                Version = 1,
                TenantId = "tenant1"
            });
        }
        DbContext.Set<QuestionVersion>().AddRange(versions);
        DbContext.SaveChanges();
    }

    #region 标签规则基础测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithSingleTagRule_ShouldSelectCorrectQuestions()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签规则测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        Assert.NotNull(result);
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.NotNull(examPaper);
        Assert.Equal(10, examPaper.ExamPaperQuestions.Count);

        // 验证标签A的题目数量（100% = 10题）
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var tagAQuestions = questions.Where(q => HasTag(q, "标签A")).ToList();
        Assert.Equal(10, tagAQuestions.Count);

        // 验证分数设置
        Assert.All(examPaper.ExamPaperQuestions, epq => Assert.Equal(5, epq.Score));
        Assert.Equal(50, examPaper.TotalScore);
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithSingleTagRule_ShouldVerifyQuestionDetails()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签详细验证测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签B", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        var questionIds = examPaper!.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 验证标签B的题目（100% = 10题）
        var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();
        Assert.Equal(10, tagBQuestions.Count);

        // 验证所有标签B的题目都是单选题
        Assert.All(tagBQuestions, q => Assert.Equal(QuestionType.SingleChoice, q.Type));

        // 验证所有题目都属于指定分类
        Assert.All(questions, q => Assert.Equal(1L, q.CategoryId));

        // 验证题目不重复
        Assert.Equal(questionIds.Count, questionIds.Distinct().Count());
    }

    #endregion

    #region 多标签规则测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithMultipleTagRules_ShouldSelectCorrectProportions()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "多标签规则测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 40 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

        // 获取题目详细信息
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 验证标签分布（精确匹配）
        var tagAQuestions = questions.Where(q => HasTag(q, "标签A")).ToList();
        var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();

        // 标签A应有6题（60%），标签B应有4题（40%）
        Assert.Equal(6, tagAQuestions.Count);
        Assert.Equal(4, tagBQuestions.Count);
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithMultipleTagRules_100Percent_ShouldMatchExactly()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "多标签100%比例测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 70 },
                new TagRule { Tag = "标签B", Percentage = 30 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        var questionIds = examPaper!.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 验证精确的标签分布：70% = 7题，30% = 3题
        var tagAQuestions = questions.Where(q => HasTag(q, "标签A")).ToList();
        var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();

        Assert.Equal(7, tagAQuestions.Count);
        Assert.Equal(3, tagBQuestions.Count);

        // 100%的标签规则，所有题目都应该有标签（验证没有无标签的题目）
        var questionsWithTags = questions.Where(q => !string.IsNullOrWhiteSpace(q.Tags)).ToList();
        Assert.Equal(10, questionsWithTags.Count);
    }

    #endregion

    #region 标签+题型组合测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithTagRulesAndMultipleTypes_ShouldDistributeCorrectly()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签+题型组合测试卷",
            TotalScore = 100,
            PassScore = 60,
            Duration = 90,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                },
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.MultipleChoice,
                    Count = 5,
                    ScorePerQuestion = 10
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 40 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(15, examPaper!.ExamPaperQuestions.Count);

        // 获取所有题目详细信息
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 验证单选题和多选题的数量
        var singleChoiceQuestions = questions.Where(q => q.Type == QuestionType.SingleChoice).ToList();
        var multipleChoiceQuestions = questions.Where(q => q.Type == QuestionType.MultipleChoice).ToList();

        Assert.Equal(10, singleChoiceQuestions.Count);
        Assert.Equal(5, multipleChoiceQuestions.Count);

        // 验证标签在各题型中的分布
        // 单选题：60% = 6题标签A，40% = 4题标签B
        var singleChoiceTagA = singleChoiceQuestions.Where(q => HasTag(q, "标签A")).ToList();
        var singleChoiceTagB = singleChoiceQuestions.Where(q => HasTag(q, "标签B")).ToList();
        Assert.Equal(6, singleChoiceTagA.Count);
        Assert.Equal(4, singleChoiceTagB.Count);

        // 多选题：60% = 3题标签A，40% = 2题标签B
        var multipleChoiceTagA = multipleChoiceQuestions.Where(q => HasTag(q, "标签A")).ToList();
        var multipleChoiceTagB = multipleChoiceQuestions.Where(q => HasTag(q, "标签B")).ToList();
        Assert.Equal(3, multipleChoiceTagA.Count);
        Assert.Equal(2, multipleChoiceTagB.Count);

        // 验证分数设置
        var singleChoiceScores = examPaper.ExamPaperQuestions
            .Where(epq => singleChoiceQuestions.Any(q => q.Id == epq.QuestionId))
            .Select(epq => epq.Score);
        Assert.All(singleChoiceScores, score => Assert.Equal(5, score));

        var multipleChoiceScores = examPaper.ExamPaperQuestions
            .Where(epq => multipleChoiceQuestions.Any(q => q.Id == epq.QuestionId))
            .Select(epq => epq.Score);
        Assert.All(multipleChoiceScores, score => Assert.Equal(10, score));
    }

    #endregion

    #region 标签+难度组合测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithTagRulesAndDifficultyRules_ShouldCombineCorrectly()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签+难度组合测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            DifficultyRules = new List<DifficultyRule>
            {
                new DifficultyRule { Difficulty = QuestionDifficulty.Easy, Percentage = 30 },
                new DifficultyRule { Difficulty = QuestionDifficulty.Medium, Percentage = 40 },
                new DifficultyRule { Difficulty = QuestionDifficulty.Hard, Percentage = 30 }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

        // 获取题目详细信息
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 所有题目都应该来自标签A
        Assert.All(questions, q => Assert.True(HasTag(q, "标签A")));

        // 验证难度分布：30% = 3题简单，40% = 4题中等，30% = 3题困难
        var easyQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Easy).ToList();
        var mediumQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Medium).ToList();
        var hardQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Hard).ToList();

        Assert.Equal(3, easyQuestions.Count);
        Assert.Equal(4, mediumQuestions.Count);
        Assert.Equal(3, hardQuestions.Count);
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithComplexTagAndDifficultyRules_ShouldDistributeCorrectly()
    {
        // Arrange - 测试标签和难度的复杂组合
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "复杂标签难度组合测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            DifficultyRules = new List<DifficultyRule>
            {
                new DifficultyRule { Difficulty = QuestionDifficulty.Easy, Percentage = 50 },
                new DifficultyRule { Difficulty = QuestionDifficulty.Hard, Percentage = 50 }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 40 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        var questionIds = examPaper!.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 验证标签分布：60% = 6题标签A，40% = 4题标签B
        var tagAQuestions = questions.Where(q => HasTag(q, "标签A")).ToList();
        var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();

        Assert.Equal(6, tagAQuestions.Count);
        Assert.Equal(4, tagBQuestions.Count);

        // 验证每个标签内的难度分布
        // 标签A：50% = 3题简单，50% = 3题困难
        var tagAEasy = tagAQuestions.Where(q => q.Difficulty == QuestionDifficulty.Easy).ToList();
        var tagAHard = tagAQuestions.Where(q => q.Difficulty == QuestionDifficulty.Hard).ToList();
        Assert.Equal(3, tagAEasy.Count);
        Assert.Equal(3, tagAHard.Count);

        // 标签B：50% = 2题简单，50% = 2题困难
        var tagBEasy = tagBQuestions.Where(q => q.Difficulty == QuestionDifficulty.Easy).ToList();
        var tagBHard = tagBQuestions.Where(q => q.Difficulty == QuestionDifficulty.Hard).ToList();
        Assert.Equal(2, tagBEasy.Count);
        Assert.Equal(2, tagBHard.Count);
    }

    #endregion

    #region 题目去重测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithMultipleTypeRules_ShouldNotDuplicateQuestions()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "题目去重测试卷",
            TotalScore = 100,
            PassScore = 60,
            Duration = 90,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                },
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.MultipleChoice,
                    Count = 5,
                    ScorePerQuestion = 10
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        var questionIds = examPaper!.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var uniqueQuestionIds = questionIds.Distinct().ToList();

        Assert.Equal(questionIds.Count, uniqueQuestionIds.Count);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithSingleQuestion_ShouldSelectCorrectly()
    {
        // Arrange - 测试只需要1题的情况
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "单题测试卷",
            TotalScore = 5,
            PassScore = 3,
            Duration = 30,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 1,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Single(examPaper!.ExamPaperQuestions);

        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var question = Assert.Single(questions);
        Assert.True(HasTag(question, "标签A"));
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithNoTagRules_ShouldSelectRandomly()
    {
        // Arrange - 测试不使用标签规则时的行为
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "无标签规则测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = null, // 不使用标签规则
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

        // 验证题目不重复
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        Assert.Equal(questionIds.Count, questionIds.Distinct().Count());
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithEmptyTagRules_ShouldSelectRandomly()
    {
        // Arrange - 测试空标签规则列表的行为
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "空标签规则测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>(), // 空列表
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);
    }

    [Fact]
    public async Task GenerateRandomExamPaper_WithMaximumQuestions_ShouldSelectAll()
    {
        // Arrange - 测试选择某标签的所有题目
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "最大题目数测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // 所有题目都应该来自标签A
        Assert.All(questions, q => Assert.True(HasTag(q, "标签A")));

        // 题目不重复
        Assert.Equal(10, questionIds.Distinct().Count());
    }

    #endregion

    #region RandomRules序列化验证测试

    [Fact]
    public async Task GenerateRandomExamPaper_ShouldSaveTagRulesToRandomRules()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "RandomRules序列化测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 40 }
            },
            DifficultyRules = new List<DifficultyRule>
            {
                new DifficultyRule { Difficulty = QuestionDifficulty.Easy, Percentage = 50 },
                new DifficultyRule { Difficulty = QuestionDifficulty.Hard, Percentage = 50 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(examPaper);
        Assert.NotNull(examPaper.RandomRules);
        
        // 反序列化验证RandomRules内容
        var randomRules = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(examPaper.RandomRules);
        Assert.True(randomRules.TryGetProperty("TagRules", out var tagRulesElement));
        
        var tagRulesArray = tagRulesElement.EnumerateArray().ToList();
        Assert.Equal(2, tagRulesArray.Count);
        
        // 验证标签A
        var tagARule = tagRulesArray.FirstOrDefault(t => t.GetProperty("Tag").GetString() == "标签A");
        Assert.True(tagARule.ValueKind != System.Text.Json.JsonValueKind.Undefined, "应该包含标签A的规则");
        Assert.Equal(60, tagARule.GetProperty("Percentage").GetInt32());
        
        // 验证标签B
        var tagBRule = tagRulesArray.FirstOrDefault(t => t.GetProperty("Tag").GetString() == "标签B");
        Assert.True(tagBRule.ValueKind != System.Text.Json.JsonValueKind.Undefined, "应该包含标签B的规则");
        Assert.Equal(40, tagBRule.GetProperty("Percentage").GetInt32());
    }

    #endregion

    #region 标签比例验证测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithTagPercentageLessThan100_ShouldThrowException()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签比例不足100%测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 50 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppServiceException>(
            () => _examPaperService.GenerateRandomExamPaperAsync(createDto));

        Assert.Contains("标签规则比例总和必须等于100%", exception.Message);
        Assert.Contains("当前为50%", exception.Message);
    }

    #endregion

    #region 验证逻辑测试

    [Fact]
    public async Task CreateRandomExamPaper_WithTagPercentageOver100_ShouldThrowException()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签比例超过100%测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 50 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppServiceException>(
            () => _examPaperService.GenerateRandomExamPaperAsync(createDto));

        Assert.Contains("标签规则比例总和必须等于100%", exception.Message);
        Assert.Contains("当前为110%", exception.Message);
    }

    [Fact]
    public async Task CreateRandomExamPaper_WithInsufficientTagQuestions_ShouldThrowException()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "标签题目不足测试卷",
            TotalScore = 100,
            PassScore = 60,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 20, // 需要20题，但标签A只有10题
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppServiceException>(
            () => _examPaperService.GenerateRandomExamPaperAsync(createDto));

        Assert.Contains("标签'标签A'", exception.Message);
        Assert.Contains("题目不足", exception.Message);
    }

    [Fact]
    public async Task CreateRandomExamPaper_WithZeroTagPercentage_ShouldIgnoreTagRule()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "零比例标签测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签B", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

        // 验证标签B的题目
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();
        Assert.True(tagBQuestions.Count >= 5, $"标签B应至少有5题，实际{tagBQuestions.Count}题");
    }

    #endregion

    #region 试卷题目顺序和排序测试

    [Fact]
    public async Task GenerateRandomExamPaper_ShouldOrderQuestionsByOrderNum()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "题目顺序测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        // 验证题目按OrderNumber排序
        var orderedQuestions = examPaper!.ExamPaperQuestions.OrderBy(epq => epq.OrderNumber).ToList();
        for (int i = 0; i < orderedQuestions.Count; i++)
        {
            Assert.Equal(i + 1, orderedQuestions[i].OrderNumber);
        }
    }

    #endregion

    #region 多次生成一致性测试

    [Fact]
    public async Task GenerateRandomExamPaper_MultipleTimes_ShouldRespectRulesConsistently()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "多次生成一致性测试卷",
            TotalScore = 50,
            PassScore = 30,
            Duration = 60,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 60 },
                new TagRule { Tag = "标签B", Percentage = 40 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act - 生成3次试卷
        var results = new List<ExamPaperDto>();
        for (int i = 0; i < 3; i++)
        {
            createDto.Name = $"多次生成一致性测试卷_{i + 1}";
            var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);
            results.Add(result);
        }

        // Assert - 每次生成都应该遵守规则
        foreach (var result in results)
        {
            var examPaper = await _examPaperRepository
                .Find(p => p.Id == result.Id)
                .Include(p => p.ExamPaperQuestions)
                .FirstOrDefaultAsync();

            Assert.Equal(10, examPaper!.ExamPaperQuestions.Count);

            var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
            var questions = await DbContext.Set<Question>()
                .Where(q => questionIds.Contains(q.Id))
                .ToListAsync();

            // 验证标签分布
            var tagAQuestions = questions.Where(q => HasTag(q, "标签A")).ToList();
            var tagBQuestions = questions.Where(q => HasTag(q, "标签B")).ToList();

            Assert.Equal(6, tagAQuestions.Count);
            Assert.Equal(4, tagBQuestions.Count);

            // 验证无重复
            Assert.Equal(10, questionIds.Distinct().Count());
        }
    }

    #endregion

    #region 跨多题型去重验证测试

    [Fact]
    public async Task GenerateRandomExamPaper_WithMultipleTypesAndTags_ShouldEnsureGlobalDeduplication()
    {
        // Arrange
        var createDto = new GenerateRandomExamPaperDto
        {
            Name = "跨题型去重测试卷",
            TotalScore = 100,
            PassScore = 60,
            Duration = 90,
            QuestionTypeRules = new List<QuestionTypeRule>
            {
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.SingleChoice,
                    Count = 10,
                    ScorePerQuestion = 5
                },
                new QuestionTypeRule
                {
                    QuestionType = QuestionType.MultipleChoice,
                    Count = 5,
                    ScorePerQuestion = 10
                }
            },
            TagRules = new List<TagRule>
            {
                new TagRule { Tag = "标签A", Percentage = 100 }
            },
            CategoryIds = new List<long> { 1 }
        };

        // Act
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);

        // Assert
        var examPaper = await _examPaperRepository
            .Find(p => p.Id == result.Id)
            .Include(p => p.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        Assert.Equal(15, examPaper!.ExamPaperQuestions.Count);

        // 验证所有题目的QuestionId都不相同（全局去重）
        var questionIds = examPaper.ExamPaperQuestions.Select(epq => epq.QuestionId).ToList();
        var distinctIds = questionIds.Distinct().ToList();

        Assert.Equal(questionIds.Count, distinctIds.Count);
        Assert.Equal(15, distinctIds.Count);

        // 验证单选题和多选题的ID没有重叠
        var questions = await DbContext.Set<Question>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var singleChoiceIds = questions.Where(q => q.Type == QuestionType.SingleChoice).Select(q => q.Id).ToList();
        var multipleChoiceIds = questions.Where(q => q.Type == QuestionType.MultipleChoice).Select(q => q.Id).ToList();

        Assert.Empty(singleChoiceIds.Intersect(multipleChoiceIds));
    }

    #endregion
}

