using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis
{
    /// <summary>
    /// 包含 CRUD 操作方法的信息的类。
    /// </summary>
    public class CrudActions
    {
        public MethodInfo Create { get; set; }
        public MethodInfo List { get; set; }
        public MethodInfo Update { get; set; }
        public MethodInfo Delete { get; set; }
        public MethodInfo QuickSave { get; set; }
        public MethodInfo Export { get; set; }

        /// <summary>
        /// 获取所有 CRUD 操作的方法集合。
        /// </summary>
        /// <returns>CRUD 操作的方法集合。</returns>
        public IEnumerable<MethodInfo> GetAllMethods()
        {
            if (Create != null) yield return Create;
            if (List != null) yield return List;
            if (Update != null) yield return Update;
            if (Delete != null) yield return Delete;
            if (QuickSave != null) yield return QuickSave;
            if (Export != null) yield return Export;
        }
    }
}
