using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class CrudHelper
    {
        public CrudActions HasCrudActions(Type controller)
        {
            var actions = new CrudActions();

            var methods = GetControllerMethods(controller);

            actions.Create = methods.FirstOrDefault(m => IsCreateMethod(m));
            actions.Read = methods.FirstOrDefault(m => IsReadMethod(m));
            actions.Update = methods.FirstOrDefault(m => IsUpdateMethod(m));
            actions.Delete = methods.FirstOrDefault(m => IsDeleteMethod(m));

            return actions;
        }

        private IEnumerable<MethodInfo> GetControllerMethods(Type controller)
        {
            return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        private bool IsCreateMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Add", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsReadMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUpdateMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Modify", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDeleteMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Remove", StringComparison.OrdinalIgnoreCase);
        }
    }
}

