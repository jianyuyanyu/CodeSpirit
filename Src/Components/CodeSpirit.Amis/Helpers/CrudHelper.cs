using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    public class CrudHelper
    {
        /// <summary>
        /// 判断指定控制器是否包含 CRUD 操作。
        /// </summary>
        /// <param name="controller">要检查的控制器类型。</param>
        /// <returns>包含 CRUD 操作的对象。</returns>
        public CrudActions HasCrudActions(Type controller)
        {
            // 初始化 CRUD 操作对象
            CrudActions actions = new();

            // 获取控制器的所有公共实例方法
            IEnumerable<MethodInfo> methods = GetControllerMethods(controller);

            // 查找符合创建操作前缀的方法
            actions.Create = FindMethodByActionPrefix(methods, ["Create", "Add", "Post"]);
            
            // 查找符合读取详情操作的方法（通常包含id参数的Get方法）
            actions.Detail = FindDetailMethod(methods);
            
            // 查找符合读取列表操作的方法（通常无id参数的Get方法）
            actions.List = FindListMethod(methods);
            
            // 查找符合更新操作前缀的方法
            actions.Update = FindMethodByActionPrefix(methods, ["Update", "Modify", "Put"]);
            
            // 查找符合删除操作前缀的方法
            actions.Delete = FindMethodByActionPrefix(methods, ["Delete", "Remove"]);
            
            //  查找快速保存方法
            actions.QuickSave = FindMethodByActionPrefix(methods, ["QuickSave"]);
            
            //  查找导出方法
            actions.Export = FindMethodByActionPrefix(methods, ["Export"]);
            
            //  查账导入方法
            actions.Import = FindMethodByActionPrefix(methods, ["Import", "BatchImport"]);
            
            return actions;
        }

        /// <summary>
        /// 获取指定控制器的所有公共实例方法（不包括基类的方法）。
        /// </summary>
        /// <param name="controller">要获取方法的控制器类型。</param>
        /// <returns>控制器的所有公共实例方法。</returns>
        private IEnumerable<MethodInfo> GetControllerMethods(Type controller)
        {
            return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        /// <summary>
        /// 查找获取单个实体详情的方法
        /// </summary>
        /// <param name="methods">控制器的所有方法集合。</param>
        /// <returns>匹配的第一个方法，如果没有找到则返回 null。</returns>
        private MethodInfo FindDetailMethod(IEnumerable<MethodInfo> methods)
        {
            // 首先查找显式命名为Detail的方法
            var explicitDetailMethod = methods.FirstOrDefault(m => 
                m.Name.StartsWith("Detail", StringComparison.OrdinalIgnoreCase) ||
                m.Name.EndsWith("Detail", StringComparison.OrdinalIgnoreCase));
            
            if (explicitDetailMethod != null)
            {
                return explicitDetailMethod;
            }
            
            // 然后查找类似GetXxx(id)格式的方法
            return methods.FirstOrDefault(m => 
            {
                // 检查方法是否以Get开头且不以List结尾
                if (m.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase) && 
                    !m.Name.EndsWith("List", StringComparison.OrdinalIgnoreCase))
                {
                    // 检查参数，通常详情方法有一个id参数
                    var parameters = m.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var paramType = parameters[0].ParameterType;
                        // 典型的id类型
                        return paramType == typeof(int) || 
                               paramType == typeof(long) || 
                               paramType == typeof(Guid) || 
                               paramType == typeof(string);
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// 查找获取实体列表的方法
        /// </summary>
        /// <param name="methods">控制器的所有方法集合。</param>
        /// <returns>匹配的第一个方法，如果没有找到则返回 null。</returns>
        private MethodInfo FindListMethod(IEnumerable<MethodInfo> methods)
        {
            // 首先查找显式命名为GetList或List的方法
            var explicitListMethod = methods.FirstOrDefault(m => 
                m.Name.EndsWith("List", StringComparison.OrdinalIgnoreCase) || 
                m.Name.StartsWith("List", StringComparison.OrdinalIgnoreCase));
            
            if (explicitListMethod != null)
            {
                return explicitListMethod;
            }
            
            // 然后查找Get前缀的无ID参数方法或接受查询条件对象的方法
            return methods.FirstOrDefault(m => 
            {
                if (m.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    var parameters = m.GetParameters();
                    // 无参数的Get方法通常是列表
                    if (parameters.Length == 0)
                    {
                        return true;
                    }
                    // 或者参数是查询条件对象（通常包含Query或Filter字样）
                    if (parameters.Length == 1)
                    {
                        var paramName = parameters[0].Name?.ToLower();
                        var paramTypeName = parameters[0].ParameterType.Name?.ToLower();
                        return (paramName?.Contains("query") == true || 
                                paramName?.Contains("filter") == true ||
                                paramTypeName?.Contains("query") == true || 
                                paramTypeName?.Contains("filter") == true);
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// 根据方法名的前缀查找匹配的控制器方法。
        /// </summary>
        /// <param name="methods">控制器的所有方法集合。</param>
        /// <param name="prefixes">方法名应匹配的前缀数组。</param>
        /// <returns>匹配的第一个方法，如果没有找到则返回 null。</returns>
        private MethodInfo FindMethodByActionPrefix(IEnumerable<MethodInfo> methods, string[] prefixes)
        {
            // 遍历所有方法，查找第一个方法名以给定前缀开头的方法
            return methods.FirstOrDefault(m => prefixes.Any(prefix => m.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
