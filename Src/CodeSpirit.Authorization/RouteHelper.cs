namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 路由辅助类
    /// </summary>
    public static class RouteHelper
    {
        /// <summary>
        /// 合并控制器和动作的路由
        /// </summary>
        /// <param name="controllerRoute">控制器路由模板</param>
        /// <param name="actionRoute">动作路由模板</param>
        /// <param name="controllerName">控制器名称</param>
        /// <returns>完整的路由路径</returns>
        public static string CombineRoutes(string controllerRoute, string actionRoute, string controllerName)
        {
            controllerRoute = string.IsNullOrWhiteSpace(controllerRoute) ? "[controller]" : controllerRoute;
            actionRoute = string.IsNullOrWhiteSpace(actionRoute) ? string.Empty : actionRoute;

            controllerRoute = controllerRoute.Replace("[controller]", controllerName);

            return !string.IsNullOrWhiteSpace(actionRoute)
                ? $"{controllerRoute.TrimEnd('/')}/{actionRoute.TrimStart('/')}"
                : controllerRoute.TrimEnd('/');
        }
    }
}
