// 文件路径: Controllers/Dtos/UserQueryDto.cs
// 文件路径: Controllers/Dtos/ApiResponse.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 列表数据封装类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ListData<T>
    {
        /// <summary>
        /// 数据项列表
        /// </summary>
        public List<T> Items { get; set; }

        /// <summary>
        /// 总数
        /// </summary>
        public int Total { get; set; }

        public ListData() { }

        public ListData(List<T> items, int total)
        {
            Items = items;
            Total = total;
        }
    }
}
