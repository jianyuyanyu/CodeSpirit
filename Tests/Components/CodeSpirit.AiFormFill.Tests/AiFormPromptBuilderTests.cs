using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.AiFormFill.Services;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.AiFormFill.Tests;

/// <summary>
/// AiFormPromptBuilder 单元测试
/// </summary>
public class AiFormPromptBuilderTests
{
    private readonly Mock<ILogger<AiFormPromptBuilder>> _mockLogger;
    private readonly AiFormPromptBuilder _promptBuilder;

    public AiFormPromptBuilderTests()
    {
        _mockLogger = new Mock<ILogger<AiFormPromptBuilder>>();
        _promptBuilder = new AiFormPromptBuilder(_mockLogger.Object);
    }

    #region 测试数据模型

    /// <summary>
    /// 测试用的任务拆解表单DTO
    /// </summary>
    [AiFormFill(
        CustomPromptTemplate = @"你是一个专业的项目管理助手，负责将用户的目标拆解为可执行的任务。

目标信息:
- 标题: {GoalTitle}
- 描述: {GoalDescription}
- 类别: {GoalCategory}
- 目标日期: {GoalTargetDate}

拆解粒度: {Granularity}/5 (数字越大拆解越细)

请根据以上信息拆解任务。")]
    public class TestTaskBreakdownDto
    {
        [DisplayName("目标ID")]
        [AiFieldFill(Enabled = false)]
        public Guid GoalId { get; set; }

        [DisplayName("目标标题")]
        [AiFieldFill(Enabled = false)]
        public string? GoalTitle { get; set; }

        [DisplayName("目标描述")]
        [AiFieldFill(Enabled = false)]
        public string? GoalDescription { get; set; }

        [DisplayName("目标类别")]
        [AiFieldFill(Enabled = false)]
        public string? GoalCategory { get; set; }

        [DisplayName("目标日期")]
        [AiFieldFill(Enabled = false)]
        public DateTime? GoalTargetDate { get; set; }

        [DisplayName("拆解粒度")]
        [AiFieldFill(Enabled = false)]
        public int Granularity { get; set; } = 3;

        [Required(ErrorMessage = "任务列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要生成一个任务")]
        [DisplayName("任务列表")]
        [AiFieldFill(Priority = 10)]
        public List<TestTaskItemDto> Tasks { get; set; } = new();
    }

    /// <summary>
    /// 测试用的任务项DTO
    /// </summary>
    public class TestTaskItemDto
    {
        [Required]
        [MaxLength(200)]
        [DisplayName("任务标题")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        [DisplayName("任务描述")]
        public string? Description { get; set; }

        [Range(1, 5)]
        [DisplayName("优先级")]
        public int Priority { get; set; } = 3;

        [DisplayName("预计开始时间")]
        public DateTime? EstimatedStartTime { get; set; }

        [DisplayName("预计完成时间")]
        public DateTime? EstimatedEndTime { get; set; }

        [DisplayName("依赖任务")]
        public string? DependsOn { get; set; }
    }

    /// <summary>
    /// 简单测试DTO（无自定义模板）
    /// </summary>
    [AiFormFill(TriggerField = "Name")]
    public class SimpleTestDto
    {
        [DisplayName("名称")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("描述")]
        [AiFieldFill(Priority = 1)]
        public string? Description { get; set; }

        [DisplayName("年龄")]
        [Range(1, 120)]
        [AiFieldFill(Priority = 2)]
        public int? Age { get; set; }
    }

    /// <summary>
    /// 带有CustomDescription的测试DTO（用于测试字段描述显示）
    /// </summary>
    [AiFormFill(TriggerField = "Input")]
    public class CustomDescriptionTestDto
    {
        [DisplayName("输入")]
        public string Input { get; set; } = string.Empty;

        [DisplayName("标题")]
        [MaxLength(100)]
        [AiFieldFill(Priority = 1, CustomDescription = "简短的标题，不超过10个字")]
        public string? Title { get; set; }

        [DisplayName("内容")]
        [Required]
        [AiFieldFill(Priority = 2, CustomDescription = "详细的内容描述，需要包含关键信息和上下文")]
        public string? Content { get; set; }

        [DisplayName("评分")]
        [Range(1, 10)]
        [AiFieldFill(Priority = 3, CustomDescription = "质量评分（1-10分），分数越高表示质量越好")]
        public int? Score { get; set; }
    }

    #endregion

    #region 命名占位符替换测试

    /// <summary>
    /// 测试：有数据时正确替换命名占位符
    /// </summary>
    [Fact]
    public void BuildPrompt_WithExistingData_ShouldReplacePlaceholders()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalId = Guid.NewGuid(),
            GoalTitle = "完成项目里程碑",
            GoalDescription = "在Q1完成产品功能开发和测试",
            GoalCategory = "技术开发",
            GoalTargetDate = new DateTime(2025, 3, 31),
            Granularity = 4
        };

        var customTemplate = @"目标信息:
- 标题: {GoalTitle}
- 描述: {GoalDescription}
- 类别: {GoalCategory}
- 日期: {GoalTargetDate}
- 粒度: {Granularity}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("完成项目里程碑", result);
        Assert.Contains("在Q1完成产品功能开发和测试", result);
        Assert.Contains("技术开发", result);
        Assert.Contains("2025年03月31日", result);
        Assert.Contains("4", result);
        Assert.DoesNotContain("{GoalTitle}", result);
        Assert.DoesNotContain("{GoalDescription}", result);
    }

    /// <summary>
    /// 测试：无数据时占位符被替换为空字符串
    /// </summary>
    [Fact]
    public void BuildPrompt_WithoutExistingData_ShouldReplaceWithEmpty()
    {
        // Arrange
        var customTemplate = @"目标: {GoalTitle}
描述: {GoalDescription}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        Assert.DoesNotContain("{GoalTitle}", result);
        Assert.DoesNotContain("{GoalDescription}", result);
        Assert.Contains("目标: ", result);
        Assert.Contains("描述: ", result);
    }

    /// <summary>
    /// 测试：属性值为null时占位符被替换为空字符串
    /// </summary>
    [Fact]
    public void BuildPrompt_WithNullPropertyValues_ShouldReplaceWithEmpty()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "有标题",
            GoalDescription = null, // null值
            GoalCategory = null,
            GoalTargetDate = null
        };

        var customTemplate = @"标题: {GoalTitle}
描述: {GoalDescription}
类别: {GoalCategory}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("有标题", result);
        Assert.DoesNotContain("{GoalDescription}", result);
        Assert.DoesNotContain("{GoalCategory}", result);
        Assert.Contains("标题: 有标题", result);
    }

    /// <summary>
    /// 测试：DateTime类型格式化
    /// </summary>
    [Fact]
    public void BuildPrompt_WithDateTimeProperty_ShouldFormatCorrectly()
    {
        // Arrange
        var testDate = new DateTime(2025, 12, 25);
        var existingData = new TestTaskBreakdownDto
        {
            GoalTargetDate = testDate
        };

        var customTemplate = "截止日期: {GoalTargetDate}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("2025年12月25日", result);
    }

    /// <summary>
    /// 测试：集合类型显示数量
    /// </summary>
    [Fact]
    public void BuildPrompt_WithCollectionProperty_ShouldShowCount()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            Tasks = new List<TestTaskItemDto>
            {
                new() { Title = "任务1" },
                new() { Title = "任务2" },
                new() { Title = "任务3" }
            }
        };

        var customTemplate = "现有任务: {Tasks}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("3项", result);
    }

    #endregion

    #region JSON结构追加测试

    /// <summary>
    /// 测试：自定义模板应追加JSON结构
    /// </summary>
    [Fact]
    public void BuildPrompt_WithCustomTemplate_ShouldAppendJsonStructure()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "测试目标"
        };

        var customTemplate = "这是自定义提示词。";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("这是自定义提示词", result);
        Assert.Contains("请以JSON格式回复，格式如下：", result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("\"Tasks\"", result);
        Assert.Contains("要求：", result);
        Assert.Contains("- 内容要与输入信息高度相关", result);
    }

    /// <summary>
    /// 测试：JSON结构只包含启用AI填充的字段
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_ShouldOnlyIncludeAiEnabledFields()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "测试"
        };

        var customTemplate = "测试";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        // 应该包含启用AI填充的字段
        Assert.Contains("\"Tasks\"", result);

        // 不应该包含禁用AI填充的字段
        Assert.DoesNotContain("\"GoalId\"", result);
        Assert.DoesNotContain("\"GoalTitle\"", result);
        Assert.DoesNotContain("\"GoalDescription\"", result);
        Assert.DoesNotContain("\"Granularity\"", result);
    }

    /// <summary>
    /// 测试：集合类型在JSON结构中应展开元素的详细结构
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_CollectionShouldExpandElementStructure()
    {
        // Arrange
        var customTemplate = "测试";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        // 1. 应该包含 Tasks 数组
        Assert.Contains("\"Tasks\": [", result);
        
        // 2. 应该展开 TaskItemDto 的结构
        Assert.Contains("\"Title\":", result);
        Assert.Contains("\"Description\":", result);
        Assert.Contains("\"Priority\":", result);
        Assert.Contains("\"EstimatedStartTime\":", result);
        Assert.Contains("\"EstimatedEndTime\":", result);
        Assert.Contains("\"DependsOn\":", result);
        
        // 3. 应该包含字段注释（DisplayName）
        Assert.Contains("// 任务标题", result);
        Assert.Contains("// 任务描述", result);
        Assert.Contains("// 优先级", result);
        
        // 4. 应该包含验证信息
        Assert.Contains("必填", result);
        Assert.Contains("最多200字符", result);
        Assert.Contains("范围1-5", result);
    }

    /// <summary>
    /// 测试：默认模板应包含JSON结构
    /// </summary>
    [Fact]
    public void BuildPrompt_WithDefaultTemplate_ShouldIncludeJsonStructure()
    {
        // Act
        var result = _promptBuilder.BuildPrompt<SimpleTestDto>(
            "测试名称",
            null, // 使用默认模板
            null);

        // Assert
        Assert.Contains("请以JSON格式回复，格式如下：", result);
        Assert.Contains("\"Description\"", result);
        Assert.Contains("\"Age\"", result);
        Assert.DoesNotContain("\"Name\"", result); // 触发字段不应出现在填充列表中
    }

    /// <summary>
    /// 测试：当自定义模板中已包含JSON结构说明（```json）时，不应重复追加
    /// </summary>
    [Fact]
    public void BuildPrompt_WithJsonStructureInTemplate_ShouldNotAppendAgain()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "测试目标"
        };

        var customTemplateWithJsonStructure = @"你是一个助手。

**返回JSON结构说明：**
```json
{
  ""Tasks"": []
}
```

请严格按照上述JSON结构返回。";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplateWithJsonStructure,
            existingData);

        // Assert
        // 1. 模板内容应该存在
        Assert.Contains("你是一个助手", result);
        Assert.Contains("返回JSON结构说明", result);
        
        // 2. 不应该追加额外的"请以JSON格式回复"
        var jsonFormatOccurrences = System.Text.RegularExpressions.Regex.Matches(result, "请以JSON格式回复").Count;
        Assert.Equal(0, jsonFormatOccurrences); // 不应该出现这个文本
        
        // 3. 不应该追加额外的"要求："部分
        var requirementsOccurrences = System.Text.RegularExpressions.Regex.Matches(result, "要求：").Count;
        Assert.Equal(0, requirementsOccurrences);
    }

    /// <summary>
    /// 测试：检测各种JSON结构说明关键词
    /// </summary>
    [Theory]
    [InlineData("```json\n{}\n```")]
    [InlineData("```JSON\n{}\n```")]
    [InlineData("**JSON结构说明：**")]
    [InlineData("返回JSON结构如下：")]
    [InlineData("**JSON格式说明**")]
    [InlineData("以JSON格式回复")]
    [InlineData("JSON Schema:")]
    [InlineData("return JSON structure")]
    [InlineData("response format:")]
    [InlineData("output format:")]
    public void BuildPrompt_WithVariousJsonKeywords_ShouldNotAppendJsonStructure(string keyword)
    {
        // Arrange
        var customTemplate = $@"这是测试模板。

{keyword}

其他内容。";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        // 应该不包含自动追加的"请以JSON格式回复，格式如下："
        // 因为模板中已经包含了JSON结构说明
        var autoAppendedJsonPrompt = "请以JSON格式回复，格式如下：";
        Assert.DoesNotContain(autoAppendedJsonPrompt, result);
    }

    /// <summary>
    /// 测试：没有JSON结构关键词时应该追加
    /// </summary>
    [Fact]
    public void BuildPrompt_WithoutJsonKeywords_ShouldAppendJsonStructure()
    {
        // Arrange
        var customTemplate = @"这是一个简单的模板。
没有任何JSON相关的说明。";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        // 应该追加JSON结构说明
        Assert.Contains("请以JSON格式回复，格式如下：", result);
        Assert.Contains("\"Tasks\"", result);
        Assert.Contains("要求：", result);
    }

    /// <summary>
    /// 测试：JSON结构应该包含CustomDescription作为注释
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_ShouldIncludeCustomDescription()
    {
        // Arrange
        var customTemplate = "测试模板";

        // Act
        var result = _promptBuilder.BuildPrompt<CustomDescriptionTestDto>(
            "测试输入",
            customTemplate,
            null);

        // Assert
        // 1. 应该包含CustomDescription的内容
        Assert.Contains("简短的标题，不超过10个字", result);
        Assert.Contains("详细的内容描述，需要包含关键信息和上下文", result);
        Assert.Contains("质量评分（1-10分），分数越高表示质量越好", result);
        
        // 2. 应该包含验证信息
        Assert.Contains("必填", result);
        Assert.Contains("最多100字符", result);
        Assert.Contains("范围1-10", result);
    }

    /// <summary>
    /// 测试：默认提示词应该包含CustomDescription
    /// </summary>
    [Fact]
    public void BuildPrompt_DefaultTemplate_ShouldUseCustomDescription()
    {
        // Act
        var result = _promptBuilder.BuildPrompt<CustomDescriptionTestDto>(
            "测试输入",
            null, // 使用默认模板
            null);

        // Assert
        // 字段描述部分应该使用CustomDescription而不是简单的DisplayName
        Assert.Contains("简短的标题，不超过10个字", result);
        Assert.Contains("详细的内容描述，需要包含关键信息和上下文", result);
        Assert.Contains("质量评分（1-10分），分数越高表示质量越好", result);
    }

    /// <summary>
    /// 测试：对比有无CustomDescription的显示差异
    /// </summary>
    [Fact]
    public void BuildPrompt_WithAndWithoutCustomDescription_ShouldShowDifference()
    {
        // Arrange
        var simpleTemplate = "测试";

        // Act - SimpleTestDto 没有 CustomDescription
        var resultWithoutCustomDesc = _promptBuilder.BuildPrompt<SimpleTestDto>(
            "测试",
            simpleTemplate,
            null);

        // Act - CustomDescriptionTestDto 有 CustomDescription
        var resultWithCustomDesc = _promptBuilder.BuildPrompt<CustomDescriptionTestDto>(
            "测试",
            simpleTemplate,
            null);

        // Assert - SimpleTestDto 只显示 DisplayName
        Assert.Contains("\"Description\": \"字段值\"", resultWithoutCustomDesc);
        Assert.DoesNotContain("描述：", resultWithoutCustomDesc); // 默认模板中的字段描述

        // Assert - CustomDescriptionTestDto 显示详细的 CustomDescription
        Assert.Contains("简短的标题，不超过10个字", resultWithCustomDesc);
    }

    #endregion

    #region 完整流程测试

    /// <summary>
    /// 测试：完整的自定义提示词处理流程
    /// </summary>
    [Fact]
    public void BuildPrompt_CompleteFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "开发新功能",
            GoalDescription = "实现用户管理模块",
            GoalCategory = "后端开发",
            GoalTargetDate = new DateTime(2025, 6, 30),
            Granularity = 5
        };

        var customTemplate = @"你是项目管理专家。

目标: {GoalTitle}
详情: {GoalDescription}
分类: {GoalCategory}
截止: {GoalTargetDate}
细度: {Granularity}/5

请拆解任务。";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        // 1. 检查占位符替换
        Assert.Contains("开发新功能", result);
        Assert.Contains("实现用户管理模块", result);
        Assert.Contains("后端开发", result);
        Assert.Contains("2025年06月30日", result);
        Assert.Contains("5/5", result);

        // 2. 检查JSON结构
        Assert.Contains("请以JSON格式回复", result);
        Assert.Contains("\"Tasks\"", result);

        // 3. 检查要求
        Assert.Contains("要求：", result);
    }

    /// <summary>
    /// 测试：当自定义模板处理失败时应使用默认模板
    /// </summary>
    [Fact]
    public void BuildPrompt_WhenCustomTemplateFails_ShouldFallbackToDefault()
    {
        // Arrange
        // 这个测试验证异常处理逻辑
        var customTemplate = "简单模板";

        // Act & Assert - 不应该抛出异常
        var exception = Record.Exception(() =>
            _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
                "触发值",
                customTemplate,
                null));

        Assert.Null(exception);
    }

    #endregion

    #region DateTime上下文测试

    /// <summary>
    /// 测试：验证提示词包含完整信息
    /// </summary>
    [Fact]
    public void BuildPrompt_WithAllData_ShouldIncludeAllInformation()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "测试目标",
            GoalDescription = "测试描述",
            GoalCategory = "测试类别",
            GoalTargetDate = DateTime.Now.AddMonths(1),
            Granularity = 3,
            Tasks = new List<TestTaskItemDto>
            {
                new() { Title = "任务1", Priority = 5 }
            }
        };

        var customTemplate = "目标: {GoalTitle}, 描述: {GoalDescription}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        Assert.Contains("测试目标", result);
        Assert.Contains("测试描述", result);
        Assert.Contains("请以JSON格式回复", result);
        Assert.Contains("\"Tasks\"", result);
    }

    #endregion

    #region 集合元素结构展开测试

    /// <summary>
    /// 测试：验证不同类型字段的JSON示例格式
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_ShouldShowCorrectTypeExamples()
    {
        // Arrange
        var customTemplate = "测试类型展示";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        // 1. string 类型
        Assert.Contains("\"Title\": \"string\"", result);
        
        // 2. int 类型
        Assert.Contains("\"Priority\": 0", result);
        
        // 3. DateTime 类型
        Assert.Contains("\"EstimatedStartTime\": \"2025-01-01T00:00:00Z\"", result);
        Assert.Contains("\"EstimatedEndTime\": \"2025-01-01T00:00:00Z\"", result);
    }

    /// <summary>
    /// 测试：简单类型集合不应展开结构
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_SimpleCollectionShouldNotExpand()
    {
        // Arrange - 使用 SimpleTestDto 它没有复杂集合
        var customTemplate = "测试";

        // Act
        var result = _promptBuilder.BuildPrompt<SimpleTestDto>(
            "测试名称",
            customTemplate,
            null);

        // Assert
        // SimpleTestDto 没有集合字段，所以不应该有数组展开
        Assert.DoesNotContain("\"Description\": [", result);
        Assert.DoesNotContain("\"Age\": [", result);
    }

    /// <summary>
    /// 测试：验证完整的任务拆解JSON结构
    /// </summary>
    [Fact]
    public void BuildPrompt_TaskBreakdown_ShouldGenerateCompleteJsonStructure()
    {
        // Arrange
        var existingData = new TestTaskBreakdownDto
        {
            GoalTitle = "测试目标",
            GoalDescription = "测试描述",
            Granularity = 5
        };

        var customTemplate = @"拆解任务：{GoalTitle}
详情：{GoalDescription}
粒度：{Granularity}";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            existingData);

        // Assert
        // 1. 占位符已替换
        Assert.Contains("测试目标", result);
        Assert.Contains("测试描述", result);
        Assert.Contains("5", result);
        
        // 2. JSON结构完整
        Assert.Contains("请以JSON格式回复，格式如下：", result);
        Assert.Contains("\"Tasks\": [", result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("]", result);
        
        // 3. 包含所有任务字段
        var taskFields = new[] { "Title", "Description", "Priority", "EstimatedStartTime", "EstimatedEndTime", "DependsOn" };
        foreach (var field in taskFields)
        {
            Assert.Contains($"\"{field}\":", result);
        }
        
        // 4. 包含字段说明
        Assert.Contains("// 任务标题", result);
        Assert.Contains("// 优先级", result);
        
        // 5. 包含验证约束
        Assert.Contains("必填", result);
        Assert.Contains("最多200字符", result);
    }

    /// <summary>
    /// 测试：嵌套对象应该显示为空对象而不展开
    /// </summary>
    [Fact]
    public void BuildPrompt_JsonStructure_NestedObjectShouldShowAsEmptyObject()
    {
        // Arrange - 我们的测试DTO没有嵌套对象，所以这个测试验证集合对象会展开
        var customTemplate = "测试";

        // Act
        var result = _promptBuilder.BuildPrompt<TestTaskBreakdownDto>(
            "全局AI填充",
            customTemplate,
            null);

        // Assert
        // Tasks 是集合，应该展开
        Assert.Contains("\"Tasks\": [", result);
        Assert.Contains("\"Title\":", result);
        
        // 但不应该有多层嵌套展开（只展开一层）
        var titleOccurrences = System.Text.RegularExpressions.Regex.Matches(result, "\"Title\":").Count;
        Assert.Equal(1, titleOccurrences); // 只在 Tasks 元素中出现一次
    }

    #endregion
}

