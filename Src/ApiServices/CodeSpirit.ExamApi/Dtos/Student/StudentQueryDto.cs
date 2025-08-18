using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 学生查询DTO
/// </summary>
public class StudentQueryDto : QueryDtoBase
{
    /// <summary>
    /// 学生组ID, -1 表示查询无分组的学生
    /// </summary>
    [DisplayName("所属分组")]
    [PageAside()]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/exam/StudentGroups/select?hasNoGroup=true",
        Multiple = false,
        //Cascade = true,
        JoinValues = true,
        ExtractValue = false,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Required = false,
        Clearable = true,
        SubmitOnChange = true,
        HeightAuto = true,
        SelectFirst = false,
        InputOnly = true,
        ShowIcon = true
    )]
    public long? StudentGroupId { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool? IsActive { get; set; }
}