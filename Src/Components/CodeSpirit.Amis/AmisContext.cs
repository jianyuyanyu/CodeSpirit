using CodeSpirit.Amis.Helpers.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CodeSpirit.Amis
{
    public class AmisContext
    {
        public string ControllerName { get; set; }
        public Type ControllerType { get; internal set; }
        public CrudActions Actions { get; internal set; }
        public Assembly Assembly { get; internal set; }
        public string BaseRoute { get; internal set; }
        public ApiRoutesInfo ApiRoutes { get; internal set; }
        public Type ListDataType { get; internal set; }

        /// <summary>
        /// 是否使用了带有 CountService 的 Tabs（用于在 CRUD 操作后刷新 Tab 数量）
        /// </summary>
        public bool HasTabCountService { get; internal set; }

        public string CrudComponentName
        {
            get
            {
                if (string.IsNullOrEmpty(ControllerName)) return null;
                return $"{ControllerName.ToCamelCase()}Crud";
            }
        }
    }

}

