namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 增强的批量导入字段特性，支持模板下载、结果展示等功能
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisEnhancedImportFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 是否创建输入表格预览
        /// </summary>
        public bool CreateInputTable { get; set; } = true;

        /// <summary>
        /// 最大导入条数限制
        /// </summary>
        public int MaxLength { get; set; } = 1000;

        /// <summary>
        /// 模板下载API路径
        /// </summary>
        public string TemplateDownloadApi { get; set; } = string.Empty;

        /// <summary>
        /// 数据提交API路径
        /// </summary>
        public string SubmitApi { get; set; } = string.Empty;

        /// <summary>
        /// 是否显示模板下载按钮
        /// </summary>
        public bool ShowTemplateDownload { get; set; } = true;

        /// <summary>
        /// 是否显示导入结果
        /// </summary>
        public bool ShowImportResult { get; set; } = true;

        /// <summary>
        /// 模板下载按钮文本
        /// </summary>
        public string TemplateDownloadText { get; set; } = "下载导入模板";

        /// <summary>
        /// 导入按钮文本
        /// </summary>
        public string ImportButtonText { get; set; } = "开始导入";

        /// <summary>
        /// 初始化 <see cref="AmisEnhancedImportFieldAttribute"/> 的新实例
        /// </summary>
        public AmisEnhancedImportFieldAttribute()
        {
            Type = "enhanced-import";
        }
    }
}
