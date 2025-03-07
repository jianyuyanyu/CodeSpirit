﻿using CodeSpirit.Amis.Helpers.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace CodeSpirit.Amis.Helpers
{
    /// <summary>
    /// 用于帮助构建 API 路由的工具类，支持常见的增删改查操作。
    /// </summary>
    public class ApiRouteHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AmisContext _amisContext;
        private readonly UtilityHelper _utilityHelper;
        private readonly IWebHostEnvironment _environment;

        public ApiRouteHelper(IHttpContextAccessor httpContextAccessor, AmisContext amisContext, UtilityHelper utilityHelper, IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _amisContext = amisContext;
            _utilityHelper = utilityHelper;
            _environment = environment;
        }

        /// <summary>
        /// 根据给定的基本路由和 CRUD 操作，生成对应的 API 路由。
        /// </summary>
        /// <param name="baseRoute">基本路由。</param>
        /// <param name="actions">CRUD 操作配置。</param>
        /// <returns>包含创建、读取、更新、删除和快速保存操作的API路由。</returns>
        public ApiRoutesInfo GetApiRoutes(string baseRoute, CrudActions actions)
        {
            return new ApiRoutesInfo(
                CreateRouteInfo(baseRoute, actions.Create, "POST"),
                CreateRouteInfo(baseRoute, actions.List, "GET"),
                CreateRouteInfo(baseRoute, actions.Update, "PUT"),
                CreateRouteInfo(baseRoute, actions.Delete, "DELETE"),
                CreateRouteInfo(baseRoute, actions.QuickSave, "PATCH"),
                CreateRouteInfo(baseRoute, actions.Export, "GET"),
                CreateRouteInfo(baseRoute, actions.Import, "POST"),
                CreateRouteInfo(baseRoute, actions.Detail, "GET")
            );
        }

        /// <summary>
        /// 创建 ApiRouteInfo 对象，封装 API 路径和 HTTP 方法。
        /// </summary>
        /// <param name="baseRoute">基本路由。</param>
        /// <param name="method">操作方法信息。</param>
        /// <param name="httpMethod">HTTP 方法。</param>
        /// <returns>封装后的 ApiRouteInfo 对象。</returns>
        private ApiRouteInfo CreateRouteInfo(string baseRoute, MethodInfo method, string httpMethod)
        {
            string template = GetRouteTemplate(method, httpMethod);
            string combinedUrl = BuildAbsoluteUrl(CombineRoutes(baseRoute, template));
            return new ApiRouteInfo(combinedUrl, httpMethod);
        }

        /// <summary>
        /// 根据当前 AmisContext 配置的基本路由和 CRUD 操作，生成对应的 API 路由。
        /// </summary>
        /// <returns>包含创建、读取、更新、删除和快速保存操作的API路由。</returns>
        public ApiRoutesInfo GetApiRoutes()
        {
            return GetApiRoutes(_amisContext.BaseRoute, _amisContext.Actions);
        }

        /// <summary>
        /// 获取控制器的基本路由。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>控制器的路由模板。</returns>
        public string GetRoute(Type controller)
        {
            // 获取路由特性，替换 [controller] 为当前控制器的名称
            RouteAttribute routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", _amisContext.ControllerName) ?? string.Empty;
        }

        /// <summary>
        /// 获取当前上下文中控制器的基本路由。
        /// </summary>
        /// <returns>当前控制器的路由模板。</returns>
        public string GetRoute()
        {
            return GetRoute(_amisContext.ControllerType);
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
            {
                return string.Empty;
            }

            // 查找方法上的 HTTP 方法特性，返回对应的路由模板
            HttpMethodAttribute attribute = method.GetCustomAttributes()
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
            HttpRequest request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return relativePath;
            }
            return $"{GetRootApi()}/{relativePath.TrimStart('/')}"; // 构建并返回绝对 URL
        }

        public string GetRootApi()
        {
            HttpRequest request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return string.Empty;
            }

            string host = request.Host.Value;
            string scheme = request.Scheme;
            
            // 在生产环境中强制使用HTTPS
            if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) && _environment.IsProduction())
            {
                scheme = "https";
            }
            
            var proxy_host = request.Headers["proxy-host"];
            if (!string.IsNullOrWhiteSpace(proxy_host))
            {
                return $"{scheme}://{host}/{proxy_host}"; // 构建并返回绝对 URL
            }
            return $"{scheme}://{host}";
        }

        /// <summary>
        /// 合并基本路由与模板，生成最终的路由路径。
        /// </summary>
        /// <param name="baseRoute">基本路由。</param>
        /// <param name="template">路由模板。</param>
        /// <returns>合并后的路由路径。</returns>
        private string CombineRoutes(string baseRoute, string template)
        {
            // 移除路由模板中的数据类型定义
            if (!string.IsNullOrEmpty(template))
            {
                template = System.Text.RegularExpressions.Regex.Replace(template, @"{\w+:[^}]+}", match =>
                {
                    var colonIndex = match.Value.IndexOf(':');
                    return match.Value.Substring(0, colonIndex) + "}";
                });
            }

            template = template?.Replace("{", "${") ?? string.Empty; // 替换模板中的 {id} 占位符
            if (string.IsNullOrEmpty(template))
            {
                return baseRoute;
            }

            return $"{baseRoute}/{template}".Replace("//", "/"); // 合并并确保不会有多余的斜杠
        }

        /// <summary>
        /// 根据方法信息返回该方法的 API 路径和请求方法。
        /// </summary>
        /// <param name="method">操作方法信息。</param>
        /// <returns>包含 API 路径和请求方法的 ApiRouteInfo 对象。</returns>
        public ApiRouteInfo GetApiRouteInfoForMethod(MethodInfo method)
        {
            if (method == null)
            {
                return new ApiRouteInfo(string.Empty, string.Empty);
            }

            // 查找方法上的 HTTP 方法特性
            HttpMethodAttribute httpMethodAttribute = method.GetCustomAttributes()
                                             .OfType<HttpMethodAttribute>()
                                             .FirstOrDefault();

            if (httpMethodAttribute == null)
            {
                return new ApiRouteInfo(string.Empty, string.Empty);
            }

            // 获取路由模板和 HTTP 方法
            string routeTemplate = httpMethodAttribute.Template;
            string httpMethod = httpMethodAttribute.HttpMethods.FirstOrDefault();

            if (string.IsNullOrEmpty(routeTemplate) || string.IsNullOrEmpty(httpMethod))
            {
                return new ApiRouteInfo(string.Empty, string.Empty);
            }

            // 结合控制器的基本路由生成完整的路由
            string baseRoute = GetRoute(method.DeclaringType);
            string fullRoute = CombineRoutes(baseRoute, routeTemplate);

            // 构建绝对 URL
            string fullUrl = BuildAbsoluteUrl(fullRoute);

            // 返回 ApiRouteInfo 对象
            return new ApiRouteInfo(fullUrl, httpMethod);
        }
    }
}
