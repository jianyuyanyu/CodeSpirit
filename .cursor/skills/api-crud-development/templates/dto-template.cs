using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.{Service}Api.Dtos.{EntityName};

/// <summary>
/// {EntityName} 展示数据传输对象
/// </summary>
public class {EntityName}Dto
{
    public long Id { get; set; }
    
    [DisplayName("{字段显示名}")]
    [TplColumn(template: "${{propertyName}}")]
    public string {PropertyName} { get; set; }
    
    [DisplayName("创建时间")]
    [DateColumn(FromNow = true)]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建 {EntityName} 数据传输对象
/// </summary>
[FormGroup("basic", "基本信息", "{FieldList}", Order = 1)]
public class Create{EntityName}Dto
{
    [Required(ErrorMessage = "{字段显示名}不能为空")]
    [MaxLength(100, ErrorMessage = "{字段显示名}长度不能超过100个字符")]
    [DisplayName("{字段显示名}")]
    [AmisInputTextField(ColumnRatio = 12)]
    public string {PropertyName} { get; set; } = string.Empty;
}

/// <summary>
/// 更新 {EntityName} 数据传输对象
/// </summary>
public class Update{EntityName}Dto
{
    [Required(ErrorMessage = "{字段显示名}不能为空")]
    [MaxLength(100, ErrorMessage = "{字段显示名}长度不能超过100个字符")]
    [DisplayName("{字段显示名}")]
    [AmisInputTextField(ColumnRatio = 12)]
    public string {PropertyName} { get; set; } = string.Empty;
}

/// <summary>
/// {EntityName} 查询数据传输对象
/// </summary>
public class {EntityName}QueryDto : QueryDtoBase
{
    [DisplayName("关键字")]
    public string? Keywords { get; set; }
    
    [DisplayName("是否激活")]
    public bool? IsActive { get; set; }
}
