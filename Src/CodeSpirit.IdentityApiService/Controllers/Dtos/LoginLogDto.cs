// Controllers/AuthController.cs
namespace CodeSpirit.IdentityApi.Controllers
{
    public partial class LoginLogsController
    {
        /// <summary>
        /// 登录日志数据传输对象。
        /// </summary>
        public class LoginLogDto
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public DateTime LoginTime { get; set; }
            public string IPAddress { get; set; }
            public bool IsSuccess { get; set; }
            public string FailureReason { get; set; }
        }
    }
}
