using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
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
            ApiRouteInfo apiRoute = apiRouteHelper.GetApiRouteInfoForMethod(method);
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
