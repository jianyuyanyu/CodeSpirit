using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 更新用户数据传输对象
/// </summary>
public class UpdateUserDto
{
    [Required]
    [MaxLength(20)]
    [Display(Name = nameof(Name), ResourceType = typeof(IdentityDisplayResources))]
    public string Name { get; set; }

    [MaxLength(18)]
    [Display(Name = nameof(IdNo), ResourceType = typeof(IdentityDisplayResources))]
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

    [Display(Name = nameof(IsActive), ResourceType = typeof(IdentityDisplayResources))]
    public bool IsActive { get; set; }

    [Display(Name = nameof(Roles), ResourceType = typeof(IdentityDisplayResources))]
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
    public string PhoneNumber { get; set; }
}
