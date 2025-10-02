using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.IdentityApi.Dtos.Department;

/// <summary>
/// 更新部门数据传输对象
/// </summary>
public class UpdateDepartmentDto
{
    /// <summary>
    /// 部门名称
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空")]
    [MaxLength(100, ErrorMessage = "部门名称长度不能超过100个字符")]
    [DisplayName("部门名称")]
    public string Name { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [Required(ErrorMessage = "部门编码不能为空")]
    [MaxLength(50, ErrorMessage = "部门编码长度不能超过50个字符")]
    [DisplayName("部门编码")]
    public string Code { get; set; }

    /// <summary>
    /// 父部门ID
    /// </summary>
    [DisplayName("父部门")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/identity/Departments/tree",
        LabelField = "name",
        ValueField = "id",
        Searchable = true,
        Clearable = true,
        ShowOutline = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门负责人ID
    /// </summary>
    [DisplayName("负责人")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/identity/Employees",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = true)]
    public long? ManagerId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [DisplayName("排序号")]
    [Range(0, int.MaxValue, ErrorMessage = "排序号必须大于等于0")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    [MaxLength(500, ErrorMessage = "部门描述长度不能超过500个字符")]
    [DisplayName("描述")]
    public string Description { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool IsActive { get; set; }
}

