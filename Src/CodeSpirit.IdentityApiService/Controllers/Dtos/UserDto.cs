public class UserDto
{
    public string Id { get; set; }
    public string Name { get; set; }

    public string Email { get; set; }

    public string UserName { get; set; }
    public string IdNo { get; set; }
    public string AvatarUrl { get; set; }
    public DateTimeOffset? LastLoginTime { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; }
}
