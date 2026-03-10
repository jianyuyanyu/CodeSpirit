using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 答题日志查询 DTO
/// </summary>
public class ExamAnswerLogQueryDto : QueryDtoBase
{
    /// <summary>
    /// 考试设置ID（必填，一次仅查询一场考试；当传入考试记录ID时可省略）
    /// </summary>
    [DisplayName("考试")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/ExamSettings/select-published",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = false,
        Placeholder = "请选择考试")]
    public long ExamSettingId { get; set; }

    /// <summary>
    /// 考试记录ID（可选，用于筛选指定考生的答题日志）
    /// </summary>
    [DisplayName("考试记录ID")]
    public long? ExamRecordId { get; set; }

    /// <summary>
    /// 考生姓名
    /// </summary>
    [DisplayName("考生姓名")]
    public string? StudentName { get; set; }

    /// <summary>
    /// 准考证号
    /// </summary>
    [DisplayName("准考证号")]
    public string? AdmissionTicket { get; set; }
}
