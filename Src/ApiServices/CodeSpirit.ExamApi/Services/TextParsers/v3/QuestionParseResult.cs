using System.Collections.Generic;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 问题解析结果
    /// </summary>
    public class QuestionParseResult
    {
        /// <summary>
        /// 问题类型
        /// </summary>
        public QuestionType Type { get; set; }

        /// <summary>
        /// 问题内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 代码片段（如果有）
        /// </summary>
        public string CodeSnippets { get; set; }

        /// <summary>
        /// 选项列表
        /// </summary>
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// 正确答案
        /// </summary>
        public string CorrectAnswer { get; set; }

        /// <summary>
        /// 解析
        /// </summary>
        public string Analysis { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 难度
        /// </summary>
        public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

        /// <summary>
        /// 分数
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 是否包含代码片段
        /// </summary>
        public bool HasCodeSnippet => !string.IsNullOrWhiteSpace(CodeSnippets);
    }
} 