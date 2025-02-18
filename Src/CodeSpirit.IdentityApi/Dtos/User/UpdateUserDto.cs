using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;

public class UpdateUserDto
{
    [Required]
    [MaxLength(20)]
    [DisplayName("姓名")]
    public string Name { get; set; }

    [MaxLength(18)]
    [DisplayName("身份证")]
    public string IdNo { get; set; }

    [MaxLength(255)]
    [DataType(DataType.ImageUrl)]
    [AmisInputImageField(
        Label = "头像",
        Receiver = "${API_HOST}/api/identity/upload/avatar",
        Accept = "image/png,image/jpeg",
        MaxSize = 1048576, // 1MB
        Multiple = false,
        Required = true,
        Placeholder = "请上传您的头像"
    )]
    public string AvatarUrl { get; set; }

    [DisplayName("是否激活")]
    public bool IsActive { get; set; }

    [DisplayName("分配角色")]
    [AmisSelectField(
        Source = "${API_HOST}/api/identity/Roles",
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

    [DisplayName("性别")]
    public Gender Gender { get; set; }

    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; }
}
