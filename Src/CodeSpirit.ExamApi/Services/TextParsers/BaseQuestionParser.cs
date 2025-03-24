using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.TextParsers;

/// <summary>
/// 基础题目解析器
/// </summary>
public abstract class BaseQuestionParser : IQuestionParser
{
    protected readonly ILogger _logger;
    
    protected BaseQuestionParser(ILogger logger)
    {
        _logger = logger;
    }

    public abstract bool CanParse(string line);
    
    public abstract QuestionParseResult Parse(IEnumerable<string> lines);

    /// <summary>
    /// 提取分数信息
    /// </summary>
    protected virtual int ExtractScore(string text, int defaultScore = 1)
    {
        // 尝试从[3分]格式提取
        var scoreMatch = Regex.Match(text, @"\[(\d+)分\]");
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out int score) && score > 0)
        {
            return score;
        }

        // 尝试从"每题X分"格式提取
        scoreMatch = Regex.Match(text, @"每[题空]\s*(\d+)\s*分");
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out score) && score > 0)
        {
            return score;
        }

        return defaultScore;
    }

    /// <summary>
    /// 提取解析信息
    /// </summary>
    protected virtual string ExtractAnalysis(IEnumerable<string> lines)
    {
        var analysisLine = lines.FirstOrDefault(l => l.Trim().StartsWith("【解析】"));
        if (analysisLine != null)
        {
            var analysis = analysisLine.Trim();
            // 移除开头的【解析】标记
            if (analysis.StartsWith("【解析】"))
            {
                analysis = analysis.Substring("【解析】".Length);
            }
            // 移除可能存在的其他标记
            analysis = Regex.Replace(analysis, @"^[\s:：]+", "");
            return analysis.Trim();
        }
        return string.Empty;
    }

    /// <summary>
    /// 提取标签信息
    /// </summary>
    protected virtual List<string> ExtractTags(IEnumerable<string> lines)
    {
        var tagLine = lines.FirstOrDefault(l => l.Trim().StartsWith("【标签】"));
        if (tagLine != null)
        {
            var tagText = tagLine.Trim();
            // 移除开头的【标签】标记
            if (tagText.StartsWith("【标签】"))
            {
                tagText = tagText.Substring("【标签】".Length);
            }
            // 移除可能存在的其他标记
            tagText = Regex.Replace(tagText, @"^[\s:：]+", "");
            return tagText.Split(new[] { '、', '，', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .Where(t => !string.IsNullOrEmpty(t))
                         .ToList();
        }
        return new List<string>();
    }

    /// <summary>
    /// 清理题目内容
    /// </summary>
    protected virtual string CleanContent(string content)
    {
        // 移除分数标记
        content = Regex.Replace(content, @"\[\d+分\]", "");
        // 移除答案标记
        content = Regex.Replace(content, @"\([A-Z]\)", "");
        content = Regex.Replace(content, @"（[A-Z]）", "");
        // 移除序号
        content = Regex.Replace(content, @"^\d+[、\.\s]+", "");
        return content.Trim();
    }

    /// <summary>
    /// 标准化选项标记
    /// </summary>
    protected virtual string NormalizeOptionMark(string mark)
    {
        mark = mark.Trim().ToUpper();
        // 处理中英文字符
        mark = mark.Replace("（", "(").Replace("）", ")");
        // 移除括号
        mark = mark.Replace("(", "").Replace(")", "");
        return mark.Trim();
    }

    /// <summary>
    /// 提取难度信息
    /// </summary>
    protected virtual QuestionDifficulty ExtractDifficulty(IEnumerable<string> lines)
    {
        var difficultyLine = lines.FirstOrDefault(l => l.Trim().StartsWith("【难度】"));
        if (difficultyLine != null)
        {
            var difficultyText = difficultyLine.Trim();
            // 移除开头的【难度】标记
            if (difficultyText.StartsWith("【难度】"))
            {
                difficultyText = difficultyText.Substring("【难度】".Length);
            }
            // 移除可能存在的其他标记
            difficultyText = Regex.Replace(difficultyText, @"^[\s:：]+", "").Trim();

            // 根据文本匹配难度级别
            return difficultyText switch
            {
                "简单" => QuestionDifficulty.Easy,
                "容易" => QuestionDifficulty.Easy,
                "中等" => QuestionDifficulty.Medium,
                "一般" => QuestionDifficulty.Medium,
                "困难" => QuestionDifficulty.Hard,
                "难" => QuestionDifficulty.Hard,
                _ => QuestionDifficulty.Medium // 默认为中等难度
            };
        }
        return QuestionDifficulty.Medium; // 如果未指定难度，默认为中等难度
    }
} 