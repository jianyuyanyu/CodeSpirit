using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel.DataAnnotations;

public class UpdateUserDto
{
    [Required]
    [MaxLength(20)]
    public string Name { get; set; }

    [MaxLength(18)]
    public string IdNo { get; set; }

    [MaxLength(255)]
    [DataType(DataType.ImageUrl)]
    public string AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public List<string> Roles { get; set; }
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; }
}
