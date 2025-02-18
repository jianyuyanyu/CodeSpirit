namespace CodeSpirit.IdentityApi.Dtos.User
{
    public class UserDiffDto
    {
        public long Id { get; set; }
        public bool? IsActive { get; set; }
        public bool? LockoutEnabled { get; set; }
    }
}