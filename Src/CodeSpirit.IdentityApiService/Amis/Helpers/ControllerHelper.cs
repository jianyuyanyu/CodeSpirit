using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Amis.Helpers
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

