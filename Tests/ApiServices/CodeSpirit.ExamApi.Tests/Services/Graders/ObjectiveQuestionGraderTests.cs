using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.Graders;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Services.Graders;

/// <summary>
/// 客观题评分器测试类
/// </summary>
public class ObjectiveQuestionGraderTests
{
    private readonly ObjectiveQuestionGrader _grader;

    /// <summary>
    /// 构造函数，初始化评分器实例
    /// </summary>
    public ObjectiveQuestionGraderTests()
    {
        _grader = new ObjectiveQuestionGrader();
    }

    /// <summary>
    /// 测试空答案记录的评分情况
    /// </summary>
    /// <remarks>
    /// 验证当没有答案记录时：
    /// 1. 总分应为0
    /// 2. 应被认为是全客观题（因为没有主观题）
    /// </remarks>
    [Fact]
    public void Grade_EmptyAnswerRecords_ReturnsZeroScore()
    {
        // Arrange
        var answerRecords = new List<ExamAnswerRecord>();

        // Act
        var result = _grader.Grade(answerRecords, 60);

        // Assert
        Assert.Equal(0, result.TotalScore);
        Assert.True(result.IsAllObjective);
    }

    /// <summary>
    /// 测试全部正确的单选题评分情况
    /// </summary>
    /// <remarks>
    /// 验证当所有单选题答案都正确时：
    /// 1. 总分应等于所有题目分数之和
    /// 2. 应被认为是全客观题
    /// 3. 每道题都应被标记为正确
    /// 4. 每道题都应获得满分
    /// </remarks>
    [Fact]
    public void Grade_AllCorrectSingleChoiceQuestions_ReturnsFullScore()
    {
        // Arrange
        var answerRecords = new List<ExamAnswerRecord>
        {
            CreateAnswerRecord("A", "A", QuestionType.SingleChoice, 2),
            CreateAnswerRecord("B", "B", QuestionType.SingleChoice, 2),
            CreateAnswerRecord("C", "C", QuestionType.SingleChoice, 2)
        };

        // Act
        var result = _grader.Grade(answerRecords, 60);

        // Assert
        Assert.Equal(6, result.TotalScore);
        Assert.True(result.IsAllObjective);
        Assert.All(answerRecords, record => Assert.True(record.IsCorrect));
        Assert.All(answerRecords, record => Assert.Equal(2, record.Score));
    }

    /// <summary>
    /// 测试混合正确和错误答案的评分情况
    /// </summary>
    /// <remarks>
    /// 验证不同题型混合且部分正确时：
    /// 1. 总分应为正确答案的分数之和
    /// 2. 应被认为是全客观题
    /// 3. 正确答案应获得满分，错误答案应得0分
    /// 4. IsCorrect标记应正确反映答案的正确性
    /// </remarks>
    [Fact]
    public void Grade_MixedCorrectAndIncorrectAnswers_ReturnsPartialScore()
    {
        // Arrange
        var answerRecords = new List<ExamAnswerRecord>
        {
            CreateAnswerRecord("A", "A", QuestionType.SingleChoice, 2),  // 正确
            CreateAnswerRecord("B", "C", QuestionType.SingleChoice, 2),  // 错误
            CreateAnswerRecord("True", "True", QuestionType.TrueFalse, 1), // 正确
            CreateAnswerRecord("A,B", "A,B", QuestionType.MultipleChoice, 3) // 正确
        };

        // Act
        var result = _grader.Grade(answerRecords, 60);

        // Assert
        Assert.Equal(6, result.TotalScore); // 2 + 0 + 1 + 3
        Assert.True(result.IsAllObjective);
        
        // 验证每个答案的评分结果
        Assert.True(answerRecords[0].IsCorrect);
        Assert.Equal(2, answerRecords[0].Score);
        
        Assert.False(answerRecords[1].IsCorrect);
        Assert.Equal(0, answerRecords[1].Score);
        
        Assert.True(answerRecords[2].IsCorrect);
        Assert.Equal(1, answerRecords[2].Score);
        
        Assert.True(answerRecords[3].IsCorrect);
        Assert.Equal(3, answerRecords[3].Score);
    }

    ///// <summary>
    ///// 测试包含主观题时的评分情况
    ///// </summary>
    ///// <remarks>
    ///// 验证当答案中包含主观题时：
    ///// 1. 总分只包含客观题的分数
    ///// 2. IsAllObjective应为false
    ///// 3. 主观题不应被评分
    ///// </remarks>
    //[Fact]
    //public void Grade_ContainsSubjectiveQuestions_ReturnsNotAllObjective()
    //{
    //    // Arrange
    //    var answerRecords = new List<ExamAnswerRecord>
    //    {
    //        CreateAnswerRecord("A", "A", QuestionType.SingleChoice, 2),
    //        CreateAnswerRecord("这是一个主观题答案", "标准答案", QuestionType.Essay, 10)
    //    };

    //    // Act
    //    var result = _grader.Grade(answerRecords, 60);

    //    // Assert
    //    Assert.Equal(2, result.TotalScore); // 只计算客观题分数
    //    Assert.False(result.IsAllObjective);
    //}

    /// <summary>
    /// 测试答案大小写不敏感的评分情况
    /// </summary>
    /// <remarks>
    /// 验证答案比较时忽略大小写：
    /// 1. 不同大小写的相同答案应被视为正确
    /// 2. 所有正确答案都应获得满分
    /// 3. IsCorrect标记应正确反映答案的正确性
    /// </remarks>
    [Fact]
    public void Grade_CaseInsensitiveAnswers_ReturnsCorrectScore()
    {
        // Arrange
        var answerRecords = new List<ExamAnswerRecord>
        {
            CreateAnswerRecord("a", "A", QuestionType.SingleChoice, 2),
            CreateAnswerRecord("TRUE", "True", QuestionType.TrueFalse, 1),
            CreateAnswerRecord("a,b", "A,B", QuestionType.MultipleChoice, 3)
        };

        // Act
        var result = _grader.Grade(answerRecords, 60);

        // Assert
        Assert.Equal(6, result.TotalScore);
        Assert.True(result.IsAllObjective);
        Assert.All(answerRecords, record => Assert.True(record.IsCorrect));
    }

    /// <summary>
    /// 测试多选题答案顺序不同的评分情况
    /// </summary>
    /// <remarks>
    /// 验证多选题答案顺序不影响评分：
    /// 1. 选项顺序不同但内容相同的答案应被视为正确
    /// 2. 所有正确答案都应获得满分
    /// 3. IsCorrect标记应正确反映答案的正确性
    /// </remarks>
    [Fact]
    public void Grade_MultipleChoiceWithDifferentOrder_ReturnsCorrectScore()
    {
        // Arrange
        var answerRecords = new List<ExamAnswerRecord>
        {
            CreateAnswerRecord("B,A,C", "A,B,C", QuestionType.MultipleChoice, 3),
            CreateAnswerRecord("C,A", "A,C", QuestionType.MultipleChoice, 3)
        };

        // Act
        var result = _grader.Grade(answerRecords, 60);

        // Assert
        Assert.Equal(6, result.TotalScore);
        Assert.True(result.IsAllObjective);
        Assert.All(answerRecords, record => Assert.True(record.IsCorrect));
    }

    /// <summary>
    /// 创建用于测试的答案记录
    /// </summary>
    /// <param name="answer">考生答案</param>
    /// <param name="correctAnswer">正确答案</param>
    /// <param name="type">题目类型</param>
    /// <param name="score">题目分值</param>
    /// <returns>答案记录实例</returns>
    private static ExamAnswerRecord CreateAnswerRecord(string answer, string correctAnswer, QuestionType type, int score)
    {
        return new ExamAnswerRecord
        {
            Answer = answer,
            Question = new Question { Type = type },
            QuestionVersion = new QuestionVersion 
            { 
                CorrectAnswer = correctAnswer,
                DefaultScore = score
            }
        };
    }
} 