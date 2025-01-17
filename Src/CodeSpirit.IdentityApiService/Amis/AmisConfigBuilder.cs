using System.ComponentModel;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class AmisConfigBuilder
    {
        private readonly ApiRouteHelper _apiRouteHelper;
        private readonly ColumnHelper _columnHelper;
        private readonly ButtonHelper _buttonHelper;
        private readonly SearchFieldHelper _searchFieldHelper;
        private readonly FormFieldHelper _formFieldHelper;
        private readonly PermissionService _permissionService;

        public AmisConfigBuilder(ApiRouteHelper apiRouteHelper, ColumnHelper columnHelper, ButtonHelper buttonHelper,
                                 SearchFieldHelper searchFieldHelper, FormFieldHelper formFieldHelper, PermissionService permissionService)
        {
            _apiRouteHelper = apiRouteHelper;
            _columnHelper = columnHelper;
            _buttonHelper = buttonHelper;
            _searchFieldHelper = searchFieldHelper;
            _formFieldHelper = formFieldHelper;
            _permissionService = permissionService;
        }

        public JObject GenerateAmisCrudConfig(string controllerName, Type controllerType, CrudActions actions)
        {
            var baseRoute = _apiRouteHelper.GetRoute(controllerType);
            var apiRoutes = _apiRouteHelper.GetApiRoutes(baseRoute, actions);
            var dataType = _apiRouteHelper.GetDataTypeFromAction(actions.Read);
            if (dataType == null)
                return null;

            var columns = _columnHelper.GetAmisColumns(dataType, controllerName, apiRoutes, actions);
            var searchFields = _searchFieldHelper.GetAmisSearchFields(actions.Read);

            var crud = new JObject
            {
                ["type"] = "crud",
                ["name"] = $"{controllerName.ToLower()}Crud",
                ["showIndex"] = true,
                //["parsePrimitiveQuery"] = new JObject
                //{
                //    ["enable"] = true,
                //    ["types"] = new JArray
                //    {
                //        "boolean","number"
                //    }
                //},
                ["api"] = new JObject
                {
                    ["url"] = apiRoutes.ReadRoute,
                    ["method"] = "get"
                },
                ["columns"] = new JArray(columns),
                //["createApi"] = new JObject
                //{
                //    ["url"] = apiRoutes.CreateRoute,
                //    ["method"] = "post"
                //},
                //["updateApi"] = new JObject
                //{
                //    ["url"] = apiRoutes.UpdateRoute,
                //    ["method"] = "put"
                //},
                //["deleteApi"] = new JObject
                //{
                //    ["url"] = apiRoutes.DeleteRoute,
                //    ["method"] = "delete"
                //},
                ["headerToolbar"] = new JArray
                {
                    _buttonHelper.CreateHeaderButton(apiRoutes.CreateRoute, actions.Create?.GetParameters())
                }
            };

            if (searchFields.Any())
            {
                crud["filter"] = new JObject
                {
                    ["title"] = "筛选",
                    ["body"] = new JObject
                    {
                        ["type"] = "group",
                        ["body"] = new JArray(searchFields)
                    }
                };
            }

            var page = new JObject
            {
                ["type"] = "page",
                ["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",
                ["body"] = crud
            };

            return page;
        }
    }
}

