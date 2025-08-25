using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.SurveyApi.Dtos.Settings;

/// <summary>
/// 问卷系统设置DTO
/// </summary>
[DisplayName("问卷系统设置")]
public class SurveySettingsDto
{
    #region LLM设置
    
    /// <summary>
    /// LLM提示词最大长度
    /// </summary>
    [DisplayName("LLM提示词最大长度")]
    [Description("生成问卷时LLM提示词的最大字符数")]
    [Range(500, 10000, ErrorMessage = "提示词长度必须在500-10000字符之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 2000)]
    public int MaxPromptLength { get; set; } = 2000;

    /// <summary>
    /// LLM最大Token数
    /// </summary>
    [DisplayName("LLM最大Token数")]
    [Description("LLM生成时的最大Token限制")]
    [Range(1000, 20000, ErrorMessage = "Token数必须在1000-20000之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 4000)]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// 启用LLM智能洞察
    /// </summary>
    [DisplayName("启用LLM智能洞察")]
    [Description("是否启用基于LLM的数据分析洞察功能")]
    [AmisSwitchField(DefaultValue = true)]
    public bool EnableLLMInsights { get; set; } = true;

    #endregion

    #region 自动保存设置

    /// <summary>
    /// 启用自动保存
    /// </summary>
    [DisplayName("启用自动保存")]
    [Description("是否启用问卷填写时的自动保存功能")]
    [AmisSwitchField(DefaultValue = true)]
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>
    /// 自动保存间隔
    /// </summary>
    [DisplayName("自动保存间隔(秒)")]
    [Description("自动保存的时间间隔，单位为秒")]
    [Range(10, 300, ErrorMessage = "保存间隔必须在10-300秒之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 30)]
    public int AutoSaveIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 草稿数据最大大小
    /// </summary>
    [DisplayName("草稿数据最大大小(KB)")]
    [Description("单个草稿数据的最大大小限制，单位为KB")]
    [Range(100, 10240, ErrorMessage = "数据大小必须在100KB-10MB之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 1024)]
    public int AutoSaveMaxDataSizeKB { get; set; } = 1024;

    /// <summary>
    /// 草稿保留天数
    /// </summary>
    [DisplayName("草稿保留天数")]
    [Description("草稿数据的保留天数，过期后自动清理")]
    [Range(1, 30, ErrorMessage = "保留天数必须在1-30天之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 7)]
    public int AutoSaveRetentionDays { get; set; } = 7;

    #endregion

    #region 默认限制设置

    /// <summary>
    /// 同一IP最大提交次数
    /// </summary>
    [DisplayName("同一IP最大提交次数")]
    [Description("默认情况下同一IP地址最多可以提交的次数")]
    [Range(1, 100, ErrorMessage = "提交次数必须在1-100次之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 1)]
    public int MaxSubmissionsPerIp { get; set; } = 1;

    /// <summary>
    /// 允许重复提交
    /// </summary>
    [DisplayName("允许重复提交")]
    [Description("默认是否允许同一用户多次提交问卷")]
    [AmisSwitchField(DefaultValue = false)]
    public bool AllowMultipleSubmissions { get; set; } = false;

    /// <summary>
    /// 默认回收量限制
    /// </summary>
    [DisplayName("默认回收量限制")]
    [Description("问卷的默认最大回收量，0表示无限制")]
    [Range(0, int.MaxValue, ErrorMessage = "回收量限制不能为负数")]
    [AmisFormField(Type = "input-number", DefaultValue = 0)]
    public int ResponseCountLimit { get; set; } = 0;

    #endregion

    #region 分析设置

    /// <summary>
    /// 分析缓存过期时间
    /// </summary>
    [DisplayName("分析缓存过期时间(分钟)")]
    [Description("问卷分析结果的缓存过期时间")]
    [Range(5, 1440, ErrorMessage = "缓存时间必须在5分钟-24小时之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 30)]
    public int AnalysisCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 启用实时分析
    /// </summary>
    [DisplayName("启用实时分析")]
    [Description("是否启用问卷数据的实时分析功能")]
    [AmisSwitchField(DefaultValue = true)]
    public bool EnableRealTimeAnalysis { get; set; } = true;

    #endregion

    #region 通知设置

    /// <summary>
    /// 启用邮件通知
    /// </summary>
    [DisplayName("启用邮件通知")]
    [Description("问卷完成时是否发送邮件通知")]
    [AmisSwitchField(DefaultValue = false)]
    public bool EnableEmailNotification { get; set; } = false;

    /// <summary>
    /// 通知邮箱地址
    /// </summary>
    [DisplayName("通知邮箱地址")]
    [Description("接收问卷通知的邮箱地址，多个邮箱用分号分隔")]
    [MaxLength(500, ErrorMessage = "邮箱地址长度不能超过500个字符")]
    [AmisFormField(Type = "input-text", DefaultValue = "")]
    public string NotificationEmails { get; set; } = "";

    /// <summary>
    /// 通知阈值
    /// </summary>
    [DisplayName("通知阈值")]
    [Description("当问卷回收量达到此数值时发送通知，0表示不设阈值")]
    [Range(0, int.MaxValue, ErrorMessage = "通知阈值不能为负数")]
    [AmisFormField(Type = "input-number", DefaultValue = 0)]
    public int NotificationThreshold { get; set; } = 0;

    #endregion
}
