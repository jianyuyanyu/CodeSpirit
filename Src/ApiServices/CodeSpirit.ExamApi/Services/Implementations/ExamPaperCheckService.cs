using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Services.Interfaces;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 试卷检查服务实现
/// </summary>
public class ExamPaperCheckService : IExamPaperCheckService, IScopedDependency
{
    /// <summary>
    /// 验证试卷和题目
    /// </summary>
    /// <param name="examPaper">试卷DTO</param>
    /// <returns>验证结果</returns>
    public ExamPaperCheckResult ValidateExamPaper(ExamPaperDto examPaper)
    {
        var result = new ExamPaperCheckResult
        {
            ExamPaperId = examPaper.Id
        };

        // 验证每道题目
        var questionContents = new Dictionary<string, List<int>>(); // 用于检测题目重复
        int questionIndex = 1;

        foreach (var question in examPaper.Questions)
        {
            var questionValidation = new QuestionCheckResult
            {
                QuestionId = question.Id,
                QuestionIndex = questionIndex
            };

            // 检查正确答案是否为空
            ValidateCorrectAnswerExists(question, questionValidation);

            // 只对单选题和多选题进行选项相关检查
            if (question.Type == QuestionType.SingleChoice || 
                question.Type == QuestionType.MultipleChoice)
            {
                ValidateQuestionOptions(question, questionValidation);
                ValidateCorrectAnswerInOptions(question, questionValidation);
            }

            // 检查题目和选项是否包含序号
            ValidateQuestionAndOptionSequence(question, questionValidation);

            // 检查题目是否包含答案
            ValidateQuestionContainsAnswer(question, questionValidation);

            // 收集题目内容用于重复检测
            if (!questionContents.ContainsKey(question.Content))
            {
                questionContents[question.Content] = new List<int>();
            }
            questionContents[question.Content].Add(questionIndex);

            if (questionValidation.HasIssues)
            {
                result.QuestionValidations[question.Id] = questionValidation;
            }

            questionIndex++;
        }

        // 检查题目重复
        foreach (var kvp in questionContents.Where(kv => kv.Value.Count > 1))
        {
            var indices = string.Join("、", kvp.Value);
            result.PaperErrors.Add($"题目内容重复：第 {indices} 题内容相同");
        }

        // 检查试卷总分和成绩换算
        if (examPaper.TotalScore > 100 && !examPaper.EnableScoreConversion)
        {
            result.PaperWarnings.Add($"试卷总分为 {examPaper.TotalScore} 分（超过100分），建议启用成绩换算功能");
        }

        // 检查题目数量
        var totalQuestions = examPaper.Questions.Count;
        if (totalQuestions != 100 && totalQuestions != 120)
        {
            result.PaperWarnings.Add($"试卷题目数量为 {totalQuestions} 题，建议设置为100题或120题");
        }

        return result;
    }

    /// <summary>
    /// 验证正确答案是否存在
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="validation">验证结果</param>
    private void ValidateCorrectAnswerExists(ExamPaperQuestionDto question, QuestionCheckResult validation)
    {
        if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            validation.Errors.Add("缺少正确答案");
        }
    }

    /// <summary>
    /// 验证题目选项
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="validation">验证结果</param>
    private void ValidateQuestionOptions(ExamPaperQuestionDto question, QuestionCheckResult validation)
    {
        if (question.Options == null || question.Options.Count == 0)
        {
            validation.Errors.Add("选项为空");
            return;
        }

        // 检查选项重复
        var duplicateOptions = question.Options
            .GroupBy(o => o)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOptions.Any())
        {
            validation.Errors.Add($"选项重复：{string.Join("、", duplicateOptions)}");
        }

        // 检查选项数量
        if (question.Options.Count < 4)
        {
            validation.Errors.Add($"选项数量少于4个（当前 {question.Options.Count} 个）");
        }
        else if (question.Options.Count > 4)
        {
            validation.Warnings.Add($"选项数量多于4个（当前 {question.Options.Count} 个）");
        }
    }

    /// <summary>
    /// 验证正确答案是否在选项中
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="validation">验证结果</param>
    private void ValidateCorrectAnswerInOptions(ExamPaperQuestionDto question, QuestionCheckResult validation)
    {
        if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            return;
        }

        // 单选题：检查正确答案是否在选项中
        if (question.Type == QuestionType.SingleChoice)
        {
            if (!question.Options.Contains(question.CorrectAnswer))
            {
                validation.Errors.Add($"正确答案 \"{question.CorrectAnswer}\" 不在选项中");
            }
        }
        // 多选题：检查所有正确答案是否都在选项中
        else if (question.Type == QuestionType.MultipleChoice)
        {
            var correctAnswers = question.CorrectAnswer.Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();

            var invalidAnswers = correctAnswers.Where(a => !question.Options.Contains(a)).ToList();
            if (invalidAnswers.Any())
            {
                validation.Errors.Add($"正确答案中的 \"{string.Join("、", invalidAnswers)}\" 不在选项中");
            }
        }
    }

    /// <summary>
    /// 验证题目和选项是否包含序号
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="validation">验证结果</param>
    private void ValidateQuestionAndOptionSequence(ExamPaperQuestionDto question, QuestionCheckResult validation)
    {
        // 检查题目文本是否带序号
        // 匹配模式：1. 2. A. B. 一、 二、 ① ② 等
        var questionSequencePattern = @"^\s*(\d+[、.．。)\)）]|[一二三四五六七八九十]+[、.．。]|[①②③④⑤⑥⑦⑧⑨⑩]|[A-Za-z][、.．。)\)）])";
        if (System.Text.RegularExpressions.Regex.IsMatch(question.Content, questionSequencePattern))
        {
            validation.Errors.Add("题目原始文本包含序号，建议移除序号");
        }

        // 检查选项文本是否带序号
        if (question.Options != null && question.Options.Any())
        {
            // 优化后的序号检测模式：
            // 1. 字母序号：A. B) C、 等（字母+分隔符+空格或非数字）
            // 2. 数字序号：限制为1-2位数字，且后面必须有空格或中文，避免误判小数和数量
            // 3. 特殊序号：圆圈数字
            var optionSequencePattern = @"^\s*([A-Za-z][、.．。)\)）]\s*|[①②③④⑤⑥⑦⑧⑨⑩]|\d{1,2}[、)\)）]\s*|\d{1,2}[.．。]\s+(?!\d))";
            var optionsWithSequence = question.Options
                .Where(opt => System.Text.RegularExpressions.Regex.IsMatch(opt, optionSequencePattern))
                .ToList();

            if (optionsWithSequence.Any())
            {
                validation.Errors.Add($"选项原始文本包含序号，建议移除序号");
            }
        }
    }

    /// <summary>
    /// 验证题目是否包含答案
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="validation">验证结果</param>
    private void ValidateQuestionContainsAnswer(ExamPaperQuestionDto question, QuestionCheckResult validation)
    {
        var content = question.Content;
        var correctAnswer = question.CorrectAnswer;

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        // 检查题目是否包含正确答案文本
        if (!string.IsNullOrWhiteSpace(correctAnswer) && content.Contains(correctAnswer, StringComparison.OrdinalIgnoreCase))
        {
            validation.Errors.Add("题目内容包含正确答案文本");
        }

        // 检查是否包含疑似选项答案的模式
        // 匹配：答案是A、答案为B、答案:C、正确答案是D、答案（A）等
        var answerPatterns = new[]
        {
            @"答案\s*[是为：:]\s*[A-Z]",
            @"答案\s*[是为：:]\s*[ABCD]",
            @"正确答案\s*[是为：:]\s*[A-Z]",
            @"答案\s*[\(（]\s*[A-Z]\s*[\)）]",
            @"选\s*[A-Z]",
            @"答案\s*[A-Z]"
        };

        foreach (var pattern in answerPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(content, pattern))
            {
                validation.Errors.Add("题目内容包含疑似答案提示（如：答案是A、正确答案：B等）");
                break; // 只报告一次
            }
        }
    }
}

