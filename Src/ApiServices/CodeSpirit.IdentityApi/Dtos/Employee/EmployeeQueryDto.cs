using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Employee;

/// <summary>
/// 职工查询数据传输对象
/// </summary>
public class EmployeeQueryDto : QueryDtoBase
{
    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// 性别筛选
    /// </summary>
    [DisplayName("性别")]
    public Gender? Gender { get; set; }

    /// <summary>
    /// 部门ID筛选
    /// </summary>
    [DisplayName("部门")]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/identity/Departments/tree",
        Multiple = false,
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
    [PageAside()]
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 在职状态筛选
    /// </summary>
    [DisplayName("在职状态")]
    public EmploymentStatus? EmploymentStatus { get; set; }

    /// <summary>
    /// 入职日期范围
    /// </summary>
    [DisplayName("入职日期")]
    public DateTime[] HireDate { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    [DisplayName("职位")]
    public string Position { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [DisplayName("职级")]
    public string JobLevel { get; set; }
}

