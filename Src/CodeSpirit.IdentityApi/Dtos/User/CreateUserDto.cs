using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;

public class CreateUserDto
{
    [Required]
    [MaxLength(20)]
    [DisplayName("姓名")]
    public string Name { get; set; }

    [Required]
    [DisplayName("用户名")]
    [MaxLength(256)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线。")]
    [Description("用户名只能包含字母、数字和下划线。")]
    public string UserName { get; set; }

    [MaxLength(18)]
    [DisplayName("身份证")]
    [RegularExpression(@"^(\d{6}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx])$", ErrorMessage = "身份证号码格式不正确。")]
    public string IdNo { get; set; }

    [MaxLength(255)]
    [DataType(DataType.ImageUrl)]
    [AmisInputImageField(
        Label = "头像",
        Receiver = "/file/api/file/images/upload",
        Accept = "image/png,image/jpeg",
        MaxSize = 1048576, // 1MB
        Multiple = false,
        Required = false,
        Placeholder = "请上传您的头像"
    )]
    public string AvatarUrl { get; set; }

    [Required]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [DisplayName("分配角色")]
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

    [DisplayName("性别")]
    public Gender Gender { get; set; }

    [DisplayName("手机号码")]
    [DataType(DataType.PhoneNumber)]
    public string PhoneNumber { get; set; }
}