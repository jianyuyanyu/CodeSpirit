using System.Reflection;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    /// <summary>
    /// 用于帮助构建 API 路由的工具类，支持常见的增删改查操作。
    /// </summary>
    public class ApiRouteHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AmisContext amisContext;
        private readonly UtilityHelper utilityHelper;

        public ApiRouteHelper(IHttpContextAccessor httpContextAccessor, AmisContext amisContext, UtilityHelper utilityHelper)
        {
            _httpContextAccessor = httpContextAccessor;
            this.amisContext = amisContext;
            this.utilityHelper = utilityHelper;
        }

        /// <summary>
        /// 根据给定的基本路由和 CRUD 操作，生成对应的 API 路由。
        /// </summary>
        /// <param name="baseRoute">基本路由。</param>
        /// <param name="actions">CRUD 操作配置。</param>
        /// <returns>包含创建、读取、更新和删除操作的 API 路由元组。</returns>
        public (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute,string QuickSaveRoute) GetApiRoutes(string baseRoute, CrudActions actions)
        {
            // 辅助方法：将模板与基本路由合并并转换为绝对 URL
            string Combine(string template) => BuildAbsoluteUrl(CombineRoutes(baseRoute, template));

            // 获取各个 CRUD 操作对应的路由模板
            var createRouteTemplate = GetRouteTemplate(actions.Create, "POST");
            var readRouteTemplate = GetRouteTemplate(actions.Read, "GET");
            var updateRouteTemplate = GetRouteTemplate(actions.Update, "PUT");
            var deleteRouteTemplate = GetRouteTemplate(actions.Delete, "DELETE");
            var quickSaveRouteTemplate = GetRouteTemplate(actions.QuickSave, "PATCH");

            // 返回各个操作的绝对 URL 路由
            return (
                Combine(createRouteTemplate),
                Combine(readRouteTemplate),
                Combine(updateRouteTemplate),
                Combine(deleteRouteTemplate),
                Combine(quickSaveRouteTemplate)
            );
        }

        /// <summary>
        /// 根据当前 AmisContext 配置的基本路由和 CRUD 操作，生成对应的 API 路由。
        /// </summary>
        /// <returns>包含创建、读取、更新和删除操作的 API 路由元组。</returns>
        public (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute, string QuickSaveRoute) GetApiRoutes()
        {
            return GetApiRoutes(amisContext.BaseRoute, amisContext.Actions);
        }

        /// <summary>
        /// 获取控制器的基本路由。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>控制器的路由模板。</returns>
        public string GetRoute(Type controller)
        {
            // 获取路由特性，替换 [controller] 为当前控制器的名称
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", amisContext.ControllerName) ?? string.Empty;
        }

        /// <summary>
        /// 获取当前上下文中控制器的基本路由。
        /// </summary>
        /// <returns>当前控制器的路由模板。</returns>
        public string GetRoute()
        {
            return GetRoute(amisContext.ControllerType);
        }

        /// <summary>
        /// 获取 HTTP 方法对应的路由模板。
        /// </summary>
        /// <param name="method">操作方法信息。</param>
        /// <param name="httpMethod">HTTP 方法（如 GET、POST 等）。</param>
        /// <returns>路由模板字符串。</returns>
        private string GetRouteTemplate(MethodInfo method, string httpMethod)
        {
            if (method == null)
                return string.Empty;

            // 查找方法上的 HTTP 方法特性，返回对应的路由模板
            var attribute = method.GetCustomAttributes()
                                  .OfType<HttpMethodAttribute>()
                                  .FirstOrDefault(a => a.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase));
            return attribute?.Template ?? string.Empty;
        }

        /// <summary>
        /// 将相对路径转换为绝对 URL。
        /// </summary>
        /// <param name="relativePath">相对路径。</param>
        /// <returns>构建的绝对 URL。</returns>
        private string BuildAbsoluteUrl(string relativePath)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return relativePath;

            var host = request.Host.Value;
            var scheme = request.Scheme;

            return $"{scheme}://{host}/{relativePath.TrimStart('/')}"; // 构建并返回绝对 URL
        }

        /// <summary>
        /// 合并基本路由与模板，生成最终的路由路径。
        /// </summary>
        /// <param name="baseRoute">基本路由。</param>
        /// <param name="template">路由模板。</param>
        /// <returns>合并后的路由路径。</returns>
        private string CombineRoutes(string baseRoute, string template)
        {
            template = template?.Replace("{id}", "${id}") ?? string.Empty; // 替换模板中的 {id} 占位符
            if (string.IsNullOrEmpty(template))
                return baseRoute;

            return $"{baseRoute}/{template}".Replace("//", "/"); // 合并并确保不会有多余的斜杠
        }

        /// <summary>
        /// 根据方法信息返回该方法的 API 路径和请求方法。
        /// </summary>
        /// <param name="method">操作方法信息。</param>
        /// <returns>一个包含 API 路径和请求方法的元组。</returns>
        public (string ApiPath, string HttpMethod) GetApiRouteInfoForMethod(MethodInfo method)
        {
            if (method == null) return (string.Empty, string.Empty);

            // 查找方法上的 HTTP 方法特性
            var httpMethodAttribute = method.GetCustomAttributes()
                                             .OfType<HttpMethodAttribute>()
                                             .FirstOrDefault();

            if (httpMethodAttribute == null)
                return (string.Empty, string.Empty);

            // 获取路由模板和 HTTP 方法
            var routeTemplate = httpMethodAttribute.Template;
            var httpMethod = httpMethodAttribute.HttpMethods.FirstOrDefault();

            if (string.IsNullOrEmpty(routeTemplate) || string.IsNullOrEmpty(httpMethod))
                return (string.Empty, string.Empty);

            // 结合控制器的基本路由生成完整的路由
            string baseRoute = GetRoute(method.DeclaringType);
            string fullRoute = CombineRoutes(baseRoute, routeTemplate);

            // 构建绝对 URL
            string fullUrl = BuildAbsoluteUrl(fullRoute);

            // 返回 API 路径和请求方法的元组
            return (fullUrl, httpMethod);
        }
    }
}
