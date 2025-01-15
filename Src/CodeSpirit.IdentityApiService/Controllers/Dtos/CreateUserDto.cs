using CodeSpirit.IdentityApi.Amis.Attributes;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class CreateUserDto
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
    public string AvatarUrl { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [DisplayName("分配角色")]
    [AmisSelectField(
            Source = "${API_HOST}/api/Roles",
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
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; }
}
