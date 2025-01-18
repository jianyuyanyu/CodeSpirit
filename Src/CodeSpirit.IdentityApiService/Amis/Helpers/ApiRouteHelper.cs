using System.Reflection;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class ApiRouteHelper
    {
        private readonly ControllerHelper _controllerHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiRouteHelper(ControllerHelper controllerHelper, IHttpContextAccessor httpContextAccessor)
        {
            _controllerHelper = controllerHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        public (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute) GetApiRoutes(string baseRoute, CrudActions actions)
        {
            string Combine(string template) => BuildAbsoluteUrl(CombineRoutes(baseRoute, template));

            var createRouteTemplate = GetRouteTemplate(actions.Create, "POST");
            var readRouteTemplate = GetRouteTemplate(actions.Read, "GET");
            var updateRouteTemplate = GetRouteTemplate(actions.Update, "PUT");
            var deleteRouteTemplate = GetRouteTemplate(actions.Delete, "DELETE");

            return (
                Combine(createRouteTemplate),
                Combine(readRouteTemplate),
                Combine(updateRouteTemplate),
                Combine(deleteRouteTemplate)
            );
        }

        /// <summary>
        /// 获取控制器的基本路由。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>基本路由字符串。</returns>
        public string GetRoute(Type controller)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", _controllerHelper.GetControllerName(controller)) ?? string.Empty;
        }

        private string GetRouteTemplate(MethodInfo method, string httpMethod)
        {
            if (method == null)
                return string.Empty;

            var attribute = method.GetCustomAttributes()
                                  .OfType<HttpMethodAttribute>()
                                  .FirstOrDefault(a => a.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase));
            return attribute?.Template ?? string.Empty;
        }

        public Type GetDataTypeFromAction(MethodInfo readMethod)
        {
            if (readMethod == null)
                return null;

            var returnType = readMethod.ReturnType;
            return ExtractDataType(GetUnderlyingType(returnType));
        }

        private Type GetUnderlyingType(Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ActionResult<>))
                {
                    return type.GetGenericArguments()[0];
                }
                if (genericDef == typeof(Task<>))
                {
                    var taskInnerType = type.GetGenericArguments()[0];
                    if (taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                    {
                        return taskInnerType.GetGenericArguments()[0];
                    }
                }
            }
            return null;
        }

        private Type ExtractDataType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ApiResponse<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(ListData<>))
                    {
                        return innerType.GetGenericArguments()[0];
                    }
                    return innerType;
                }
                if (genericDef == typeof(ListData<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        private string BuildAbsoluteUrl(string relativePath)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return relativePath;

            var host = request.Host.Value;
            var scheme = request.Scheme;

            return $"{scheme}://{host}/{relativePath.TrimStart('/')}";
        }

        private string CombineRoutes(string baseRoute, string template)
        {
            template = template?.Replace("{id}", "${id}") ?? string.Empty;
            if (string.IsNullOrEmpty(template))
                return baseRoute;

            return $"{baseRoute}/{template}".Replace("//", "/");
        }
    }
}

