using System.Reflection;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class AmisApiHelper
    {
        private readonly ApiRouteHelper apiRouteHelper;

        public AmisApiHelper(ApiRouteHelper apiRouteHelper)
        {
            this.apiRouteHelper = apiRouteHelper;
        }

        public JObject CreateApiForMethod(MethodInfo method)
        {
            var (apiPath, httpMethod) = apiRouteHelper.GetApiRouteInfoForMethod(method);
            return CreateApi(apiPath, httpMethod);
        }

        public JObject CreateApi(string apiPath, string httpMethod)
        {
            return new JObject
            {
                ["url"] = apiPath,
                ["method"] = httpMethod
            };
        }
    }
}
