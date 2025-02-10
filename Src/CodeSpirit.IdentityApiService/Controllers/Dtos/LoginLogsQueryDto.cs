// 文件路径: Controllers/Dtos/UserQueryDto.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 登录日志查询参数
    /// </summary>
    public class LoginLogsQueryDto : QueryDtoBase
    {
        [DisplayName("用户名")]
        public string UserName { get; set; }

        [DisplayName("是否登录成功")]
        public bool? IsSuccess { get; set; }
    }
}

