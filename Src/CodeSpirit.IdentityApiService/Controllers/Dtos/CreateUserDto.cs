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

    //[Required]
    //public string Password { get; set; }

    public List<string> Roles { get; set; }
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; }
}
