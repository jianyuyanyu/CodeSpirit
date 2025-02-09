// 文件路径: Controllers/Dtos/UserQueryDto.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    public class LoginLogsQueryDto : QueryDtoBase
    {
        public string UserName {  get; set; }

        public bool? IsSuccess {  get; set; }
    }
}

