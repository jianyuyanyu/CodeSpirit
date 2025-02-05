namespace CodeSpirit.Amis.Helpers.Dtos
{
    /// <summary>
    /// 封装所有CRUD操作的API路由。
    /// </summary>
    public class ApiRoutesInfo
    {
        public ApiRouteInfo Create { get; set; }
        public ApiRouteInfo Read { get; set; }
        public ApiRouteInfo Update { get; set; }
        public ApiRouteInfo Delete { get; set; }
        public ApiRouteInfo QuickSave { get; set; }
        public ApiRouteInfo Export { get; set; }

        public ApiRoutesInfo(ApiRouteInfo create, ApiRouteInfo read, ApiRouteInfo update, ApiRouteInfo delete, ApiRouteInfo quickSave, ApiRouteInfo export = null)
        {
            Create = create;
            Read = read;
            Update = update;
            Delete = delete;
            QuickSave = quickSave;
            Export = export;
        }
    }
}
