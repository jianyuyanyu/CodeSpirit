using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis
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
            var dataType = utilityHelper.GetDataTypeFromMethod(actions.List);
            if (dataType == null)
                return null;
            amisContext.ListDataType = dataType;

            // 获取列配置和搜索字段
            var columns = _columnHelper.GetAmisColumns();
            var searchFields = _searchFieldHelper.GetAmisSearchFields(actions.List);

            // 构建 CRUD 配置
            var crudConfig = new JObject
            {
                ["type"] = "crud",  // 设置类型为 CRUD
                ["name"] = $"{controllerName.ToLower()}Crud",  // 设置配置名称
                ["showIndex"] = true,  // 显示索引列
                ["api"] = amisApiHelper.CreateApi(apiRoutes.Read),  // 设置 API 配置
                ["quickSaveApi"] = amisApiHelper.CreateApi(apiRoutes.QuickSave),
                ["columns"] = new JArray(columns),  // 设置列
                ["headerToolbar"] = BuildHeaderToolbar(apiRoutes.Create, actions.Create?.GetParameters(), actions),  // 设置头部工具栏
                ["footerToolbar"] = new JArray()
                {
                    "switch-per-page",
                    "pagination",
                    "statistics"
                }
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
                ["body"] = new JArray()
                {
                    //new JObject {
                    //    ["type"] = "chart",
                    //    ["api"] = "${API_HOST}/api/userStatistics/usergrowth-and-active-users"
                    //},
                    crudConfig
                }
            };

            return pageConfig;
        }

        #region 辅助方法

        /// <summary>
        /// 构建头部工具栏配置。
        /// </summary>
        private JArray BuildHeaderToolbar(ApiRouteInfo createRoute, IEnumerable<ParameterInfo> createParameters, CrudActions actions)
        {
            var buttons = new JArray();
            if (createRoute != null && actions.Create != null)
            {

                buttons.Add(_buttonHelper.CreateHeaderButton(createRoute, createParameters));
            }
            buttons.Add(new JObject()
            {
                ["type"] = "export-excel",
                ["label"] = "导出当前页",
                //["filename"] = ""
            });

            if (actions.Export != null)
            {
                buttons.Add(new JObject()
                {
                    ["type"] = "export-excel",
                    ["label"] = "导出全部",
                    ["api"] = new JObject
                    {
                        ["url"] = amisContext.ApiRoutes.Export.ApiPath,
                        ["method"] = amisContext.ApiRoutes.Export.HttpMethod
                    },
                });
            }
            return buttons;
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
