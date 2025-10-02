using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Department;

/// <summary>
/// 部门批量导入项数据传输对象
/// </summary>
public class DepartmentBatchImportItemDto
{
    /// <summary>
    /// 部门名称
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空")]
    [MaxLength(100, ErrorMessage = "部门名称长度不能超过100个字符")]
    [DisplayName("部门名称")]
    [JsonProperty("部门名称")]
    public string Name { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [Required(ErrorMessage = "部门编码不能为空")]
    [MaxLength(50, ErrorMessage = "部门编码长度不能超过50个字符")]
    [DisplayName("部门编码")]
    [JsonProperty("部门编码")]
    public string Code { get; set; }

    /// <summary>
    /// 父部门编码
    /// </summary>
    [DisplayName("父部门编码")]
    [JsonProperty("父部门编码")]
    public string ParentCode { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [DisplayName("排序号")]
    [JsonProperty("排序号")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    [MaxLength(500, ErrorMessage = "部门描述长度不能超过500个字符")]
    [DisplayName("描述")]
    [JsonProperty("描述")]
    public string Description { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    [JsonProperty("是否激活")]
    public bool IsActive { get; set; } = true;
}

