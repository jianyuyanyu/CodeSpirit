using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.Question
{
    /// <summary>
    /// 题目导入步骤请求DTO
    /// </summary>
    public class QuestionImportStepDto
    {
        /// <summary>
        /// 当前步骤：parse=解析预览, import=确认导入
        /// </summary>
        [DisplayName("操作步骤")]
        public string Step { get; set; } = "parse";

        /// <summary>
        /// 分类ID
        /// </summary>
        [Required(ErrorMessage = "请选择题目分类")]
        [DisplayName("分类")]
        [AmisTreeSelectField(
            DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
            Multiple = false,
            Cascade = true,
            ShowOutline = true,
            LabelField = "name",
            ValueField = "id"
        )]
        public long CategoryId { get; set; }

        /// <summary>
        /// Word格式文本（解析步骤需要）
        /// </summary>
        [DisplayName("Word格式文本")]
        [AmisFormField(type: "editor", 
            Placeholder = @"一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【难度】困难
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信", 
            AdditionalConfig = "{\"language\":\"markdown\",\"size\":\"xxl\"}",
            VisibleOn = "this.step === 'parse'"
        )]
        public string? Text { get; set; }

        /// <summary>
        /// 是否启用AI审核
        /// </summary>
        [DisplayName("启用AI审核")]
        [AmisFormField(type: "switch", VisibleOn = "this.step === 'parse'")]
        public bool EnableAiAudit { get; set; } = true;

        /// <summary>
        /// 是否自动修正错误
        /// </summary>
        [DisplayName("自动修正错误")]
        [AmisFormField(type: "switch", VisibleOn = "this.step === 'parse'")]
        public bool AutoCorrectErrors { get; set; } = true;

        /// <summary>
        /// 预览会话ID（导入步骤需要）
        /// </summary>
        [DisplayName("预览会话ID")]
        public string? PreviewSessionId { get; set; }

        /// <summary>
        /// 要导入的题目索引列表（为空表示导入所有）
        /// </summary>
        [DisplayName("导入题目索引")]
        public List<int>? QuestionIndexes { get; set; }

        /// <summary>
        /// 用户修改的题目数据
        /// </summary>
        [DisplayName("修改的题目")]
        public List<QuestionPreviewDto>? ModifiedQuestions { get; set; }
    }

    /// <summary>
    /// 题目导入步骤响应DTO
    /// </summary>
    public class QuestionImportStepResponseDto
    {
        /// <summary>
        /// 当前步骤
        /// </summary>
        [DisplayName("当前步骤")]
        public string Step { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        [DisplayName("是否成功")]
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        [DisplayName("消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 预览数据（解析步骤返回）
        /// </summary>
        [DisplayName("预览数据")]
        public QuestionBatchPreviewResponseDto? PreviewData { get; set; }

        /// <summary>
        /// 导入结果（导入步骤返回）
        /// </summary>
        [DisplayName("导入结果")]
        public ImportResultDto? ImportResult { get; set; }
    }

    /// <summary>
    /// 导入结果DTO
    /// </summary>
    public class ImportResultDto
    {
        /// <summary>
        /// 成功导入数量
        /// </summary>
        [DisplayName("成功导入数量")]
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        [DisplayName("失败数量")]
        public int FailedCount { get; set; }

        /// <summary>
        /// 失败项目列表
        /// </summary>
        [DisplayName("失败项目")]
        public List<string> FailedItems { get; set; } = new();
    }

    /// <summary>
    /// 题目预览DTO
    /// </summary>
    public class QuestionPreviewDto
    {
        /// <summary>
        /// 题目内容
        /// </summary>
        [DisplayName("题目内容")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 题目类型
        /// </summary>
        [DisplayName("题目类型")]
        public QuestionType Type { get; set; }

        /// <summary>
        /// 选项列表
        /// </summary>
        [DisplayName("选项")]
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// 正确答案
        /// </summary>
        [DisplayName("正确答案")]
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 解析
        /// </summary>
        [DisplayName("解析")]
        public string? Analysis { get; set; }

        /// <summary>
        /// 知识点
        /// </summary>
        [DisplayName("知识点")]
        public string? KnowledgePoints { get; set; }

        /// <summary>
        /// 难度
        /// </summary>
        [DisplayName("难度")]
        public QuestionDifficulty Difficulty { get; set; }

        /// <summary>
        /// 默认分数
        /// </summary>
        [DisplayName("默认分数")]
        public decimal DefaultScore { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        [DisplayName("标签")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// AI审核状态
        /// </summary>
        [DisplayName("审核状态")]
        public AuditStatus AuditStatus { get; set; } = AuditStatus.NotAudited;

        /// <summary>
        /// 是否有错误
        /// </summary>
        [DisplayName("是否有错误")]
        public bool HasErrors { get; set; }

        /// <summary>
        /// 审核消息
        /// </summary>
        [DisplayName("审核消息")]
        public string? AuditMessage { get; set; }

        /// <summary>
        /// 是否已被AI修正
        /// </summary>
        [DisplayName("是否已修正")]
        public bool IsCorrected { get; set; }

        /// <summary>
        /// 修正前的原始题目内容
        /// </summary>
        [DisplayName("原始题目内容")]
        public string? OriginalContent { get; set; }

        /// <summary>
        /// 修正前的原始选项
        /// </summary>
        [DisplayName("原始选项")]
        public List<string>? OriginalOptions { get; set; }

        /// <summary>
        /// 修正前的原始答案
        /// </summary>
        [DisplayName("原始答案")]
        public string? OriginalAnswer { get; set; }

        /// <summary>
        /// 修正前的原始解析
        /// </summary>
        [DisplayName("原始解析")]
        public string? OriginalAnalysis { get; set; }

        /// <summary>
        /// 修正说明列表
        /// </summary>
        [DisplayName("修正说明")]
        public List<string> CorrectionNotes { get; set; } = new();
    }

    /// <summary>
    /// 题目批量预览响应DTO
    /// </summary>
    public class QuestionBatchPreviewResponseDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        [DisplayName("分类ID")]
        public long CategoryId { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        [DisplayName("分类名称")]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 预览会话ID
        /// </summary>
        [DisplayName("会话ID")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 题目列表
        /// </summary>
        [DisplayName("题目列表")]
        public List<QuestionPreviewDto> Questions { get; set; } = new();

        /// <summary>
        /// 解析错误列表
        /// </summary>
        [DisplayName("解析错误")]
        public List<string> ParseErrors { get; set; } = new();

        /// <summary>
        /// AI审核摘要
        /// </summary>
        [DisplayName("AI审核摘要")]
        public AiAuditSummaryDto AiAuditSummary { get; set; } = new();
    }

    /// <summary>
    /// AI审核摘要DTO
    /// </summary>
    public class AiAuditSummaryDto
    {
        /// <summary>
        /// 总题目数
        /// </summary>
        [DisplayName("总题目数")]
        public int TotalCount { get; set; }

        /// <summary>
        /// 通过审核数量
        /// </summary>
        [DisplayName("通过数量")]
        public int PassedCount { get; set; }

        /// <summary>
        /// 有错误数量
        /// </summary>
        [DisplayName("错误数量")]
        public int ErrorCount { get; set; }

        /// <summary>
        /// 已修正数量
        /// </summary>
        [DisplayName("修正数量")]
        public int CorrectedCount { get; set; }
    }

    /// <summary>
    /// AI审核状态枚举
    /// </summary>
    public enum AuditStatus
    {
        /// <summary>
        /// 未审核
        /// </summary>
        NotAudited = 0,

        /// <summary>
        /// 审核通过
        /// </summary>
        Passed = 1,

        /// <summary>
        /// 有错误
        /// </summary>
        HasErrors = 2,

        /// <summary>
        /// 审核失败
        /// </summary>
        Failed = 3
    }

    /// <summary>
    /// 题目批量导入确认DTO
    /// </summary>
    public class QuestionBatchImportConfirmDto
    {
        /// <summary>
        /// 预览会话ID
        /// </summary>
        [Required(ErrorMessage = "预览会话ID不能为空")]
        [DisplayName("预览会话ID")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 要导入的题目索引列表（为空表示导入所有）
        /// </summary>
        [DisplayName("导入题目索引")]
        public List<int>? QuestionIndexes { get; set; }

        /// <summary>
        /// 用户修改的题目数据
        /// </summary>
        [DisplayName("修改的题目")]
        public List<QuestionPreviewDto>? Questions { get; set; }
    }
}
