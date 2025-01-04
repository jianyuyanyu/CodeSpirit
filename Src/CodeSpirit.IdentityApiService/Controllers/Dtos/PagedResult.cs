// Controllers/AuthController.cs
namespace CodeSpirit.IdentityApi.Controllers
{
    public partial class LoginLogsController
    {
        /// <summary>
        /// 分页结果模型。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        public class PagedResult<T>
        {
            public IEnumerable<T> Items { get; set; }
            public int TotalRecords { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }
    }
}
