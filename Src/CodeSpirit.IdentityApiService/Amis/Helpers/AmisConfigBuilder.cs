using System.ComponentModel;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    /// <summary>
    /// 负责生成 AMIS CRUD 配置的构建器。
    /// </summary>
    public class AmisConfigBuilder
    {
        // 依赖注入的助手类
        private readonly ApiRouteHelper _apiRouteHelper;
        private readonly ColumnHelper _columnHelper;
        private readonly ButtonHelper _buttonHelper;
        private readonly SearchFieldHelper _searchFieldHelper;
        private readonly AmisContext amisContext;
        private readonly UtilityHelper utilityHelper;
        private readonly AmisApiHelper amisApiHelper;

        /// <summary>
        /// 构造函数，初始化所需的助手类。
        /// </summary>
        public AmisConfigBuilder(ApiRouteHelper apiRouteHelper, ColumnHelper columnHelper, ButtonHelper buttonHelper,
                                 SearchFieldHelper searchFieldHelper, AmisContext amisContext, UtilityHelper utilityHelper, AmisApiHelper amisApiHelper)
        {
            _apiRouteHelper = apiRouteHelper;
            _columnHelper = columnHelper;
            _buttonHelper = buttonHelper;
            _searchFieldHelper = searchFieldHelper;
            this.amisContext = amisContext;
            this.utilityHelper = utilityHelper;
            this.amisApiHelper = amisApiHelper;
        }

        /// <summary>
        /// 生成 AMIS 的 CRUD 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称</param>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="actions">CRUD 操作类型</param>
        /// <returns>返回 AMIS 配置的 JSON 对象</returns>
        public JObject GenerateAmisCrudConfig(string controllerName, Type controllerType, CrudActions actions)
        {
            // 获取基础路由信息
            var baseRoute = _apiRouteHelper.GetRoute();
            amisContext.BaseRoute = baseRoute;

            var apiRoutes = _apiRouteHelper.GetApiRoutes();
            amisContext.ApiRoutes = apiRoutes;

            // 获取读取数据的类型，如果类型为空，则返回空
            var dataType = utilityHelper.GetDataTypeFromMethod(actions.Read);
            if (dataType == null)
                return null;
            amisContext.ListDataType = dataType;

            // 获取列配置和搜索字段
            var columns = _columnHelper.GetAmisColumns();
            var searchFields = _searchFieldHelper.GetAmisSearchFields(actions.Read);

            // 构建 CRUD 配置
            var crudConfig = new JObject
            {
                ["type"] = "crud",  // 设置类型为 CRUD
                ["name"] = $"{controllerName.ToLower()}Crud",  // 设置配置名称
                ["showIndex"] = true,  // 显示索引列
                ["api"] = amisApiHelper.CreateApi(apiRoutes.ReadRoute, "get"),  // 设置 API 配置
                ["quickSaveApi"] = amisApiHelper.CreateApi(apiRoutes.QuickSaveRoute, "patch"),
                ["columns"] = new JArray(columns),  // 设置列
                ["headerToolbar"] = BuildHeaderToolbar(apiRoutes.CreateRoute, actions.Create?.GetParameters())  // 设置头部工具栏
            };

            // 如果有搜索字段，加入筛选配置
            if (searchFields.Any())
            {
                crudConfig["filter"] = BuildFilterConfig(searchFields);
            }

            // 构建页面配置
            var pageConfig = new JObject
            {
                ["type"] = "page",  // 设置页面类型
                ["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",  // 设置页面标题
                ["body"] = crudConfig  // 设置页面主体为 CRUD 配置
            };

            return pageConfig;
        }

        #region 辅助方法

        /// <summary>
        /// 构建头部工具栏配置。
        /// </summary>
        private JArray BuildHeaderToolbar(string createRoute, IEnumerable<ParameterInfo> createParameters)
        {
            return new JArray
            {
                _buttonHelper.CreateHeaderButton(createRoute, createParameters)  // 创建按钮
            };
        }

        /// <summary>
        /// 构建筛选配置对象。
        /// </summary>
        private JObject BuildFilterConfig(IEnumerable<JObject> searchFields)
        {
            return new JObject
            {
                ["title"] = "筛选",  // 筛选标题
                ["body"] = new JObject
                {
                    ["type"] = "group",  // 筛选类型为组合
                    ["body"] = new JArray(searchFields)  // 添加搜索字段
                }
            };
        }

        internal JObject GenerateAmisCrudConfig()
        {
            return GenerateAmisCrudConfig(amisContext.ControllerName, amisContext.ControllerType, amisContext.Actions);
        }

        #endregion
    }
}
