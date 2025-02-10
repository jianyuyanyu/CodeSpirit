using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Authorization;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly AmisContext amisContext;
        private readonly ApiRouteHelper apiRouteHelper;
        private readonly AmisApiHelper amisApiHelper;
        private readonly FormFieldHelper formFieldHelper;

        public ButtonHelper(IPermissionService permissionService, AmisContext amisContext, ApiRouteHelper apiRouteHelper, AmisApiHelper amisApiHelper, FormFieldHelper formFieldHelper)
        {
            _permissionService = permissionService;
            this.amisContext = amisContext;
            this.apiRouteHelper = apiRouteHelper;
            this.amisApiHelper = amisApiHelper;
            this.formFieldHelper = formFieldHelper;
        }

        // 创建一个通用的按钮模板
        private JObject CreateButton(string label, string actionType, JObject dialogOrDrawer = null, JObject api = null, string confirmText = null, bool? download = null, string visibleOn = null)
        {
            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = label,
                ["actionType"] = actionType
            };

            if (dialogOrDrawer != null)
            {
                button["dialog"] = dialogOrDrawer;
            }

            if (api != null)
            {
                button["api"] = api;
            }

            if (!string.IsNullOrEmpty(confirmText))
            {
                button["confirmText"] = confirmText;
            }

            if (download.HasValue && download.Value)
            {
                button["download"] = true;
            }

            if (!string.IsNullOrEmpty(visibleOn))
            {
                button["visibleOn"] = visibleOn;
            }

            return button;
        }

        // 创建“新增”按钮
        public JObject CreateHeaderButton(string title = "新增", ApiRouteInfo route = null, IEnumerable<ParameterInfo> formParameters = null, string size = null)
        {
            JObject dialogBody = new()
            {
                ["title"] = title,
                ["size"] = size,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromParameters(formParameters))
                },
            };

            return CreateButton(title, "dialog", dialogOrDrawer: dialogBody);
        }

        // 创建“编辑”按钮
        public JObject CreateEditButton(ApiRouteInfo updateRoute, IEnumerable<ParameterInfo> updateParameters)
        {
            string title = "编辑";
            JObject drawerBody = new()
            {
                ["title"] = title,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = updateRoute.ApiPath,
                        ["method"] = updateRoute.HttpMethod
                    },
                    ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromParameters(updateParameters))
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        // 创建“删除”按钮
        public JObject CreateDeleteButton(ApiRouteInfo deleteRoute)
        {
            JObject api = new()
            {
                ["url"] = deleteRoute.ApiPath,
                ["method"] = deleteRoute.HttpMethod
            };

            return CreateButton("删除", "ajax", api: api, confirmText: "确定要删除吗？");
        }

        // 获取自定义操作按钮
        public List<JObject> GetCustomOperationsButtons()
        {
            List<JObject> buttons = [];
            // 获取当前类型的所有方法
            MethodInfo[] methods = amisContext.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            // 查找带有 [Operation] 特性的所有方法
            foreach (MethodInfo method in methods)
            {
                OperationAttribute op = method.GetCustomAttribute<OperationAttribute>();
                if (op != null)
                {
                    // 为每个操作方法创建按钮
                    JObject button = CreateCustomOperationButton(op, method);

                    // Add redirect configuration if specified
                    if (op.ActionType == "ajax" && !string.IsNullOrEmpty(op.Redirect))
                    {
                        button["redirect"] = op.Redirect;
                    }
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        // 创建自定义操作按钮
        public JObject CreateCustomOperationButton(OperationAttribute op, MethodInfo method)
        {
            JObject api = amisApiHelper.CreateApiForMethod(method);
            if (api["url"] == null)
            {
                api["url"] = op.Api;
            }

            return CreateButton(op.Label, op.ActionType, api: api, confirmText: op.ConfirmText, download: op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase), visibleOn: op.VisibleOn);
        }
    }
}
