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
            CrudActions actions = new CrudActions();

            // 获取控制器的所有公共实例方法
            IEnumerable<MethodInfo> methods = GetControllerMethods(controller);

            // 查找符合创建操作前缀的方法
            actions.Create = FindMethodByActionPrefix(methods, ["Create", "Add", "Post"]);
            // 查找符合读取操作前缀的方法
            actions.List = FindMethodByActionPrefix(methods, ["Get"]);
            // 查找符合更新操作前缀的方法
            actions.Update = FindMethodByActionPrefix(methods, ["Update", "Modify", "Put"]);
            // 查找符合删除操作前缀的方法
            actions.Delete = FindMethodByActionPrefix(methods, ["Delete", "Remove"]);
            //  查找快速保存方法
            actions.QuickSave = FindMethodByActionPrefix(methods, ["QuickSave"]);
            //  查找导出方法
            actions.Export = FindMethodByActionPrefix(methods, ["Export"]);
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
