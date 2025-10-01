using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;

namespace CodeSpirit.Shared.Dtos.Common
{
    /// <summary>
    /// 增强的批量导入数据基础DTO类
    /// </summary>
    /// <typeparam name="T">要导入的数据类型</typeparam>
    public class EnhancedBatchImportDtoBase<T>
    {
        /// <summary>
        /// Excel导入的数据集合
        /// </summary>
        /// <remarks>
        /// 使用AmisEnhancedImportField特性配置增强的Excel上传控件：
        /// - 支持模板下载
        /// - 限制最大导入条数
        /// - 显示导入结果
        /// - 支持失败记录复制和导出
        /// </remarks>
        [AmisEnhancedImportField(
            Label = "批量导入数据", 
            Placeholder = "请先下载模板，填写数据后上传Excel文件",
            MaxLength = 1000,
            ShowTemplateDownload = true,
            ShowImportResult = true,
            TemplateDownloadText = "下载导入模板",
            ImportButtonText = "开始导入"
        )]
        [DisplayName("导入数据")]
        public List<T> ImportData { get; set; } = new List<T>();
    }

    /// <summary>
    /// 批量导入结果DTO
    /// </summary>
    public class BatchImportResultDto
    {
        /// <summary>
        /// 导入ID（用于跟踪导入状态）
        /// </summary>
        public string ImportId { get; set; } = string.Empty;

        /// <summary>
        /// 成功导入数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 导入状态
        /// </summary>
        public ImportStatus Status { get; set; }

        /// <summary>
        /// 导入消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 失败记录详情
        /// </summary>
        public List<ImportFailedRecord> FailedRecords { get; set; } = new List<ImportFailedRecord>();

        /// <summary>
        /// 导入开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 导入完成时间
        /// </summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 导入失败记录
    /// </summary>
    public class ImportFailedRecord
    {
        /// <summary>
        /// 行号
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 错误字段
        /// </summary>
        public List<string> ErrorFields { get; set; } = new List<string>();
    }

    /// <summary>
    /// 导入状态枚举
    /// </summary>
    public enum ImportStatus
    {
        /// <summary>
        /// 处理中
        /// </summary>
        [Description("处理中")]
        Processing = 1,

        /// <summary>
        /// 成功
        /// </summary>
        [Description("成功")]
        Success = 2,

        /// <summary>
        /// 部分成功
        /// </summary>
        [Description("部分成功")]
        PartialSuccess = 3,

        /// <summary>
        /// 失败
        /// </summary>
        [Description("失败")]
        Failed = 4
    }
}
