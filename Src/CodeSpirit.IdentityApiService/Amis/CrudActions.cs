using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis
{
    /// <summary>
    /// 包含 CRUD 操作方法的信息的类。
    /// </summary>
    public class CrudActions
    {
        public MethodInfo Create { get; set; }
        public MethodInfo Read { get; set; }
        public MethodInfo Update { get; set; }
        public MethodInfo Delete { get; set; }

        /// <summary>
        /// 获取所有 CRUD 操作的方法集合。
        /// </summary>
        /// <returns>CRUD 操作的方法集合。</returns>
        public IEnumerable<MethodInfo> GetAllMethods()
        {
            if (Create != null) yield return Create;
            if (Read != null) yield return Read;
            if (Update != null) yield return Update;
            if (Delete != null) yield return Delete;
        }
    }
}
