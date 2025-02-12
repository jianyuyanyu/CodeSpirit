namespace CodeSpirit.IdentityApi.Controllers.Dtos.User
{
    public class QuickSaveRequestDto
    {
        public List<UserDto> Rows { get; set; }
        public List<UserDiffDto> RowsDiff { get; set; }
        public long[] Ids { get; set; }
        public List<UserDto> UnModifiedItems { get; set; }
    }
}