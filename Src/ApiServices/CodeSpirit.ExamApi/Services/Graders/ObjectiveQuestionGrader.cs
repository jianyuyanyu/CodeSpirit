using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Services.Graders;

/// <summary>
/// 客观题评分器
/// </summary>
public class ObjectiveQuestionGrader
{
    /// <summary>
    /// 评分结果
    /// </summary>
    public class GradingResult
    {
        /// <summary>
        /// 总分
        /// </summary>
        public double TotalScore { get; set; }
        
        /// <summary>
        /// 是否全部为客观题
        /// </summary>
        public bool IsAllObjective { get; set; }
    }

    /// <summary>
    /// 对客观题进行评分
    /// </summary>
    /// <param name="answerRecords">答案记录列表</param>
    /// <param name="passScore">及格分数</param>
    /// <returns>评分结果</returns>
    public GradingResult Grade(IEnumerable<ExamAnswerRecord> answerRecords, double passScore)
    {
        var objectiveQuestionTypes = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse };
        double totalScore = 0;

        // 筛选客观题
        var allAnswers = answerRecords.ToList();
        var objectiveAnswers = allAnswers
            .Where(a => a.Question != null && objectiveQuestionTypes.Contains(a.Question.Type))
            .ToList();

        // 评分
        foreach (var answer in objectiveAnswers)
        {
            bool isCorrect = false;
            
            if (answer.Question.Type == QuestionType.MultipleChoice)
            {
                // 多选题答案比较：将答案拆分、排序后比较
                var studentAnswers = answer.Answer?.Split(',')
                    .Select(a => a.Trim())
                    .OrderBy(a => a)
                    .ToArray() ?? Array.Empty<string>();
                    
                var correctAnswers = answer.QuestionVersion.CorrectAnswer?.Split(',')
                    .Select(a => a.Trim())
                    .OrderBy(a => a)
                    .ToArray() ?? Array.Empty<string>();

                isCorrect = studentAnswers.SequenceEqual(correctAnswers, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // 单选题和判断题直接比较
                isCorrect = string.Equals(
                    answer.Answer?.Trim(), 
                    answer.QuestionVersion.CorrectAnswer?.Trim(), 
                    StringComparison.OrdinalIgnoreCase
                );
            }

            answer.IsCorrect = isCorrect;
            answer.Score = isCorrect ? Convert.ToInt32(answer.QuestionVersion.DefaultScore) : 0;
            totalScore += answer.Score ?? 0;
        }

        // 检查是否全部为客观题
        var subjectiveAnswersCount = allAnswers
            .Count(a => a.Question != null && !objectiveQuestionTypes.Contains(a.Question.Type));

        return new GradingResult
        {
            TotalScore = totalScore,
            IsAllObjective = subjectiveAnswersCount == 0
        };
    }
} 