namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    public class QuickSaveRequestDto
    {
        public List<UserDto> Rows { get; set; }
        public List<UserDiffDto> RowsDiff { get; set; }
        public string Ids { get; set; }
        public List<UserDto> UnModifiedItems { get; set; }
    }
}