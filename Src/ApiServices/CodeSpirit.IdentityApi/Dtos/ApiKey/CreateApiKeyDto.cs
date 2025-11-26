using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// 创建API密钥请求DTO
/// </summary>
public class CreateApiKeyDto
{
    /// <summary>
    /// API密钥名称
    /// </summary>
    [Required(ErrorMessage = "密钥名称不能为空")]
    [StringLength(100, ErrorMessage = "密钥名称长度不能超过100个字符")]
    [DisplayName("密钥名称")]
    [AmisInputTextField(Placeholder = "请输入密钥名称", ColumnRatio = 12)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API密钥描述
    /// </summary>
    [StringLength(500, ErrorMessage = "密钥描述长度不能超过500个字符")]
    [DisplayName("密钥描述")]
    [AmisTextareaField(Placeholder = "请输入密钥描述", MaxRows = 3, ColumnRatio = 12)]
    public string? Description { get; set; }

    /// <summary>
    /// 关联用户ID（可选，不填写则为当前登录用户创建）
    /// </summary>
    [DisplayName("关联用户")]
    [Description("选择为哪个用户创建API密钥，留空则为当前登录用户创建")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/identity/Users",
        ValueField = "id",
        LabelField = "name",
        Multiple = false,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择用户或留空为自己创建",
        ColumnRatio = 12
    )]
    public long? UserId { get; set; }

    /// <summary>
    /// 过期时间（可选，不设置表示永不过期）
    /// </summary>
    [DisplayName("过期时间")]
    [AmisDatetimeField(
        DisplayFormat = "YYYY-MM-DD HH:mm:ss",
        InputPlaceholder = "请选择过期时间",
        Clearable = true,
        ColumnRatio = 12
    )]
    public DateTime? ExpiresAt { get; set; }
}

