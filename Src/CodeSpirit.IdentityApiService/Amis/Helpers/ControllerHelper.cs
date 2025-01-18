using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class ControllerHelper
    {
        private readonly Assembly _assembly;

        public ControllerHelper(Assembly assembly)
        {
            _assembly = assembly;
        }

        public Type GetControllerType(string controllerName)
        {
            return _assembly.GetTypes()
                            .FirstOrDefault(t => IsValidController(t, controllerName));
        }

        private bool IsValidController(Type type, string controllerName)
        {
            return type.IsClass
                && !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && type.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase);
        }

        public string GetRoute(Type controller)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", GetControllerName(controller)) ?? string.Empty;
        }

        public string GetControllerName(Type controller)
        {
            return controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
        }
    }
}

