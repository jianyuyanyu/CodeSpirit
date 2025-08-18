using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 答卷导出设置DTO
/// </summary>
[DisplayName("答卷导出设置")]
public class ExamPaperExportSettingsDto
{
    /// <summary>
    /// 是否启用水印
    /// </summary>
    [DisplayName("启用水印")]
    [Description("开启后，导出的PDF答卷将包含水印信息")]
    [AmisSwitchField(DefaultValue = true)]
    public bool EnableWatermark { get; set; } = true;

    /// <summary>
    /// 水印文本
    /// </summary>
    [DisplayName("水印文本")]
    [Description("水印显示的文本内容，支持变量：{StudentName}、{ExamName}、{DateTime}")]
    [MaxLength(100, ErrorMessage = "水印文本长度不能超过100个字符")]
    [AmisFormField(Type = "input-text", DefaultValue = "{StudentName} - {ExamName}")]
    public string WatermarkText { get; set; } = "{StudentName} - {ExamName}";

    /// <summary>
    /// 水印透明度
    /// </summary>
    [DisplayName("水印透明度")]
    [Description("水印的透明度，范围0.1-1.0，数值越小越透明")]
    [Range(0.1, 1.0, ErrorMessage = "透明度必须在0.1-1.0之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 0.3)]
    public double WatermarkOpacity { get; set; } = 0.3;

    /// <summary>
    /// 水印字体大小
    /// </summary>
    [DisplayName("水印字体大小")]
    [Description("水印文字的字体大小，单位：像素")]
    [Range(10, 100, ErrorMessage = "字体大小必须在10-100之间")]
    [AmisFormField(Type = "input-number", DefaultValue = 24)]
    public int WatermarkFontSize { get; set; } = 24;

    /// <summary>
    /// 水印颜色
    /// </summary>
    [DisplayName("水印颜色")]
    [Description("水印文字的颜色，支持十六进制颜色代码")]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "请输入有效的十六进制颜色代码")]
    [AmisFormField(Type = "input-color", DefaultValue = "#CCCCCC")]
    public string WatermarkColor { get; set; } = "#CCCCCC";

    /// <summary>
    /// 水印旋转角度
    /// </summary>
    [DisplayName("水印旋转角度")]
    [Description("水印文字的旋转角度，单位：度")]
    [Range(-180, 180, ErrorMessage = "旋转角度必须在-180到180度之间")]
    [AmisFormField(Type = "input-number", DefaultValue = -45)]
    public int WatermarkRotation { get; set; } = -45;

    /// <summary>
    /// 是否显示考生照片
    /// </summary>
    [DisplayName("显示考生照片")]
    [Description("在答卷中显示考生照片（如果有）")]
    [AmisSwitchField(DefaultValue = false)]
    public bool ShowStudentPhoto { get; set; } = false;

    /// <summary>
    /// 是否显示二维码
    /// </summary>
    [DisplayName("显示二维码")]
    [Description("在答卷中显示包含考试信息的二维码")]
    [AmisSwitchField(DefaultValue = false)]
    public bool ShowQrCode { get; set; } = false;

    /// <summary>
    /// 二维码内容模板
    /// </summary>
    [DisplayName("二维码内容")]
    [Description("二维码包含的信息模板，支持变量：{StudentId}、{ExamRecordId}、{ExamName}")]
    [MaxLength(200, ErrorMessage = "二维码内容长度不能超过200个字符")]
    [AmisFormField(Type = "textarea", DefaultValue = "考试记录ID:{ExamRecordId},学生ID:{StudentId}")]
    public string QrCodeContent { get; set; } = "考试记录ID:{ExamRecordId},学生ID:{StudentId}";

    /// <summary>
    /// 页眉文本
    /// </summary>
    [DisplayName("页眉文本")]
    [Description("每页顶部显示的文本，支持变量替换")]
    [MaxLength(100, ErrorMessage = "页眉文本长度不能超过100个字符")]
    [AmisFormField(Type = "input-text", DefaultValue = "")]
    public string HeaderText { get; set; } = "";

    /// <summary>
    /// 页脚文本
    /// </summary>
    [DisplayName("页脚文本")]
    [Description("每页底部显示的文本，支持变量替换")]
    [MaxLength(100, ErrorMessage = "页脚文本长度不能超过100个字符")]
    [AmisFormField(Type = "input-text", DefaultValue = "第 {PageNumber} 页，共 {TotalPages} 页")]
    public string FooterText { get; set; } = "第 {PageNumber} 页，共 {TotalPages} 页";

    /// <summary>
    /// 是否显示答案解析
    /// </summary>
    [DisplayName("显示答案解析")]
    [Description("在答卷中显示题目的答案解析")]
    [AmisSwitchField(DefaultValue = false)]
    public bool ShowAnalysis { get; set; } = false;

    /// <summary>
    /// 是否显示评分详情
    /// </summary>
    [DisplayName("显示评分详情")]
    [Description("显示每道题的得分情况和评分详情")]
    [AmisSwitchField(DefaultValue = true)]
    public bool ShowScoreDetails { get; set; } = true;

    /// <summary>
    /// PDF页面方向
    /// </summary>
    [DisplayName("页面方向")]
    [Description("PDF文档的页面方向")]
    public PdfOrientation PageOrientation { get; set; } = PdfOrientation.Portrait;

    /// <summary>
    /// PDF页面大小
    /// </summary>
    [DisplayName("页面大小")]
    [Description("PDF文档的页面大小")]
    public PdfPageSize PageSize { get; set; } = PdfPageSize.A4;
}

/// <summary>
/// PDF页面方向枚举
/// </summary>
public enum PdfOrientation
{
    /// <summary>
    /// 纵向
    /// </summary>
    [Description("纵向")]
    Portrait = 0,

    /// <summary>
    /// 横向
    /// </summary>
    [Description("横向")]
    Landscape = 1
}

/// <summary>
/// PDF页面大小枚举
/// </summary>
public enum PdfPageSize
{
    /// <summary>
    /// A4
    /// </summary>
    [Description("A4")]
    A4 = 0,

    /// <summary>
    /// A3
    /// </summary>
    [Description("A3")]
    A3 = 1,

    /// <summary>
    /// Letter
    /// </summary>
    [Description("Letter")]
    Letter = 2
} 