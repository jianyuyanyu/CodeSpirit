using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 创建用户数据传输对象
/// </summary>
[AiFormFill(TriggerField = nameof(Name), IgnoreFields = new[] { nameof(AvatarUrl), nameof(Roles) })]
public class CreateUserDto
{
    [Required]
    [MaxLength(20)]
    [Display(Name = nameof(Name), ResourceType = typeof(IdentityDisplayResources))]
    public string Name { get; set; }

    [Required]
    [Display(Name = nameof(UserName), ResourceType = typeof(IdentityDisplayResources))]
    [MaxLength(256)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线。")]
    [LocalizedDescription(
        "用户名只能包含字母、数字和下划线。",
        ResourceKey = "Description.User.UserName",
        ResourceType = typeof(IdentityDisplayResources)
    )]
    [AiFieldFill(Weight = 3, Priority = 1)]
    public string UserName { get; set; }

    [MaxLength(18)]
    [Display(Name = nameof(IdNo), ResourceType = typeof(IdentityDisplayResources))]
    [RegularExpression(@"^(\d{6}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx])$", ErrorMessage = "身份证号码格式不正确。")]
    public string IdNo { get; set; }

    [MaxLength(255)]
    [DataType(DataType.ImageUrl)]
    [AmisInputImageField(
        Label = "头像",
        Receiver = "/file/api/file/images/upload?BucketName=avatar",
        Accept = "image/png,image/jpeg",
        MaxSize = 1048576, // 1MB
        Multiple = false,
        Required = false,
        Placeholder = "请上传您的头像"
    )]
    public string AvatarUrl { get; set; }

    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = nameof(Email), ResourceType = typeof(IdentityDisplayResources))]
    public string Email { get; set; }

    [Display(Name = nameof(Roles), ResourceType = typeof(IdentityDisplayResources))]
    [LocalizedDescription(
        "为用户分配相应的角色以授予权限",
        ResourceKey = "Description.User.Roles",
        ResourceType = typeof(IdentityDisplayResources)
    )]
    [AmisSelectField(
            Source = "${ROOT_API}/api/identity/Roles",
            ValueField = "name",
            LabelField = "name",
            Multiple = true,
            JoinValues = false,
            ExtractValue = true,
            Searchable = true,
            Clearable = true,
            Placeholder = "请选择角色"
        )]
    public List<string> Roles { get; set; }

    [Display(Name = nameof(Gender), ResourceType = typeof(IdentityDisplayResources))]
    public Gender Gender { get; set; }

    [Display(Name = nameof(PhoneNumber), ResourceType = typeof(IdentityDisplayResources))]
    [DataType(DataType.PhoneNumber)]
    public string PhoneNumber { get; set; }
}