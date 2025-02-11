namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    public class UserDiffDto
    {
        public string Id { get; set; }
        public bool? IsActive { get; set; }
        public bool? LockoutEnabled { get; set; }
    }
}