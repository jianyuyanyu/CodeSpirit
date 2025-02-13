using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    public class ControllerHelper
    {
        private readonly AmisContext amisContext;

        public ControllerHelper(AmisContext amisContext)
        {
            this.amisContext = amisContext;
        }

        public Type GetControllerType(string controllerName)
        {
            return amisContext.Assembly.GetTypes()
                .Where(p => p.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(t => IsValidController(t, controllerName));
        }

        public Type GetControllerType()
        {
            return GetControllerType(amisContext.ControllerName);
        }

        private bool IsValidController(Type type, string controllerName)
        {
            return type.IsClass
                && !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && type.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase);
        }

        public string GetControllerRoutePrefix(Type controllerType)
        {
            RouteAttribute routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
            if (routeAttribute != null)
            {
                string template = routeAttribute.Template;
                return template.Replace("[controller]", GetControllerName(controllerType));
            }
            return $"api/{GetControllerName(controllerType)}";
        }

        public string GetControllerDisplayName(Type controllerType)
        {
            // 优先使用 Display 特性
            DisplayAttribute displayAttribute = controllerType.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
            {
                return displayAttribute.Name;
            }

            // 其次使用 Description 特性
            DisplayNameAttribute displayNameAttribute = controllerType.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute != null && !string.IsNullOrEmpty(displayNameAttribute.DisplayName))
            {
                return displayNameAttribute.DisplayName;
            }

            // 最后使用控制器名称
            return GetControllerName(controllerType).ToSpacedWords();
        }

        public string GetMethodRoute(MethodInfo methodInfo)
        {
            // 检查方法上的 Route 特性
            RouteAttribute routeAttribute = methodInfo.GetCustomAttribute<RouteAttribute>();
            if (routeAttribute != null)
            {
                return routeAttribute.Template;
            }

            // 检查 HTTP 谓词特性 (HttpGet, HttpPost 等)
            Attribute httpMethodAttribute = methodInfo.GetCustomAttributes()
                .FirstOrDefault(attr =>
                    attr.GetType().Namespace == "Microsoft.AspNetCore.Mvc" &&
                    attr.GetType().Name.StartsWith("Http"));

            if (httpMethodAttribute != null)
            {
                string template = httpMethodAttribute.GetType()
                    .GetProperty("Template")
                    ?.GetValue(httpMethodAttribute) as string;

                if (!string.IsNullOrEmpty(template))
                {
                    return template;
                }
            }

            // 默认使用方法名转换为 kebab-case
            return methodInfo.Name.Replace("Get", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("Post", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("Put", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("Delete", "", StringComparison.OrdinalIgnoreCase)
                           .ToKebabCase();
        }

        public string GetMethodDisplayName(MethodInfo methodInfo)
        {
            // 优先使用 Display 特性
            DisplayAttribute displayAttribute = methodInfo.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
            {
                return displayAttribute.Name;
            }

            // 其次使用 Description 特性
            DescriptionAttribute descriptionAttribute = methodInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                return descriptionAttribute.Description;
            }

            // 最后使用方法名转换
            return methodInfo.Name
                .Replace("Get", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Post", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Put", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Delete", "", StringComparison.OrdinalIgnoreCase)
                .ToSpacedWords();
        }

        public string GetControllerName(Type controller)
        {
            return controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
        }

        public string GetRoute(Type controller)
        {
            RouteAttribute routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", GetControllerName(controller)) ?? string.Empty;
        }
    }
}

