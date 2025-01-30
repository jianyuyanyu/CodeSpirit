
using CodeSpirit.IdentityApi.Amis.Helpers.Dtos;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis
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
    }

}

