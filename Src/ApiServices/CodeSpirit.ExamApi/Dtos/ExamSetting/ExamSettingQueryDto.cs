using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamSetting;

/// <summary>
/// 考试设置查询条件
/// </summary>
public class ExamSettingQueryDto : QueryDtoBase
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/ExamPapers/select-published",
        ValueField = "id",
        LabelField = "name",
        Multiple = false,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择试卷"
    )]
    public long? ExamPaperId { get; set; }
    
    /// <summary>
    /// 开始时间范围起始
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTimeFrom { get; set; }
    
    /// <summary>
    /// 开始时间范围结束
    /// </summary>
    [DisplayName("-")]
    public DateTime? StartTimeTo { get; set; }
    
    /// <summary>
    /// 结束时间范围起始
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTimeFrom { get; set; }
    
    /// <summary>
    /// 结束时间范围结束
    /// </summary>
    [DisplayName("-")]
    public DateTime? EndTimeTo { get; set; }
    
    /// <summary>
    /// 最小通过率
    /// </summary>
    [DisplayName("最小通过率")]
    public decimal? MinPassRate { get; set; }

    /// <summary>
    /// 最大通过率
    /// </summary>
    [DisplayName("最大通过率")]
    public decimal? MaxPassRate { get; set; }
} 