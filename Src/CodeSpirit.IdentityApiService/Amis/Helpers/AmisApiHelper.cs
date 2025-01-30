using System.Reflection;
using CodeSpirit.IdentityApi.Amis.Helpers.Dtos;
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
            var apiRoute = apiRouteHelper.GetApiRouteInfoForMethod(method);
            return CreateApi(apiRoute);
        }

        public JObject CreateApi(ApiRouteInfo apiRoute)
        {
            return new JObject
            {
                ["url"] = apiRoute.ApiPath,
                ["method"] = apiRoute.HttpMethod
            };
        }
    }
}
