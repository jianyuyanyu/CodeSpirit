#nullable enable

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Department;

/// <summary>
/// 生成组织结构请求DTO（使用AI全局填充模式）
/// </summary>
[AiFormFill(
    GlobalFillPrompt = "智能生成组织结构",
    TriggerField = nameof(Description),
    MaxTokens = 2000,
    EnableCache = false)]
public class GenerateOrganizationStructureDto
{
    /// <summary>
    /// 组织描述（用户输入的自定义提示词）
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("组织描述")]
    [Description("请描述您的组织结构，例如：一个中型软件公司，包含技术部、产品部、市场部等")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 部门数量
    /// </summary>
    [Range(1, 20)]
    [DisplayName("部门数量")]
    [Description("需要生成的部门数量")]
    [AiFieldFill(Priority = 1, Weight = 2)]
    public int DepartmentCount { get; set; } = 5;

    /// <summary>
    /// 生成的部门列表
    /// </summary>
    [DisplayName("部门列表")]
    [Description("AI生成的部门数据列表，包含部门编码、名称、描述、层级关系等信息")]
    [AiFieldFill(Priority = 1, Weight = 3, CustomDescription = "生成完整的部门层级结构，包含部门编码（使用大写字母，如TECH、HR等）、名称、描述、父部门关系、排序等信息")]
    [AmisTableField(
        Addable = true,
        Editable = true,
        Removable = true,
        Copyable = false,
        ShowIndex = true,
        Draggable = true)]
    public List<GeneratedDepartmentItemDto> Departments { get; set; } = new();
}

/// <summary>
/// AI生成的部门项DTO
/// </summary>
public class GeneratedDepartmentItemDto
{
    /// <summary>
    /// 部门编码（必须唯一，建议使用大写字母）
    /// </summary>
    [Required(ErrorMessage = "部门编码不能为空")]
    [StringLength(50, ErrorMessage = "部门编码长度不能超过50个字符")]
    [DisplayName("部门编码")]
    [Description("部门的唯一标识编码，建议使用大写字母，如TECH、HR、SALES等")]
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空")]
    [StringLength(100, ErrorMessage = "部门名称长度不能超过100个字符")]
    [DisplayName("部门名称")]
    [Description("部门的显示名称")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序号
    /// </summary>
    [Range(0, 999, ErrorMessage = "排序号必须在0到999之间")]
    [DisplayName("排序号")]
    [Description("部门显示的排序顺序，数字越小越靠前")]
    [JsonProperty("sortOrder")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    [Description("部门是否启用，默认为true")]
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 父部门编码（如果是顶层部门则为空）
    /// </summary>
    [StringLength(50, ErrorMessage = "父部门编码长度不能超过50个字符")]
    [DisplayName("父部门编码")]
    [Description("父部门的编码，如果是顶层部门则留空")]
    [JsonProperty("parentCode")]
    public string? ParentCode { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    [DisplayName("部门描述")]
    [Description("部门的职责和功能描述")]
    [JsonProperty("description")]
    public string? Description { get; set; }
}

