using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly PermissionService _permissionService;
        private readonly AmisContext amisContext;
        private readonly ApiRouteHelper apiRouteHelper;

        public ButtonHelper(IPermissionService permissionService, AmisContext amisContext, ApiRouteHelper apiRouteHelper)
        {
            _permissionService = (PermissionService)permissionService;
            this.amisContext = amisContext;
            this.apiRouteHelper = apiRouteHelper;
        }

        // 创建一个通用的按钮模板
        private JObject CreateButton(string label, string actionType, JObject dialogOrDrawer = null, JObject api = null, string confirmText = null, bool? download = null, string visibleOn = null)
        {
            var button = new JObject
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
        public JObject CreateHeaderButton(string createRoute, IEnumerable<ParameterInfo> createParameters)
        {
            var title = "新增";
            var dialogBody = new JObject
            {
                ["title"] = title,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = createRoute,
                        ["method"] = "post"
                    },
                    ["controls"] = new JArray(new FormFieldHelper(_permissionService, new UtilityHelper()).GetAmisFormFieldsFromParameters(createParameters))
                },
            };

            return CreateButton(title, "dialog", dialogOrDrawer: dialogBody);
        }

        // 创建“编辑”按钮
        public JObject CreateEditButton(string updateRoute, IEnumerable<ParameterInfo> updateParameters)
        {
            var title = "编辑";
            var drawerBody = new JObject
            {
                ["title"] = title,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = updateRoute,
                        ["method"] = "put"
                    },
                    ["controls"] = new JArray(new FormFieldHelper(_permissionService, new UtilityHelper()).GetAmisFormFieldsFromParameters(updateParameters))
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        // 创建“删除”按钮
        public JObject CreateDeleteButton(string deleteRoute)
        {
            var api = new JObject
            {
                ["url"] = deleteRoute,
                ["method"] = "delete"
            };

            return CreateButton("删除", "ajax", api: api, confirmText: "确定要删除吗？");
        }

        // 获取自定义操作按钮
        public List<JObject> GetCustomOperationsButtons()
        {
            var buttons = new List<JObject>();
            // 获取当前类型的所有方法
            var methods = amisContext.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            // 查找带有 [Operation] 特性的所有方法
            foreach (var method in methods)
            {
                var operationAttribute = method.GetCustomAttribute<OperationAttribute>();
                if (operationAttribute != null)
                {
                    // 为每个操作方法创建按钮
                    var button = CreateCustomOperationButton(operationAttribute, method);
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        // 创建自定义操作按钮
        public JObject CreateCustomOperationButton(OperationAttribute op, MethodInfo method)
        {
            var (apiPath, httpMethod) = apiRouteHelper.GetApiRouteInfoForMethod(method);
            var api = new JObject
            {
                ["url"] = op.Api ?? apiPath,
                ["method"] = op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase) ? "get" : (op.Api ?? httpMethod)
            };

            return CreateButton(op.Label, op.ActionType, api: api, confirmText: op.ConfirmText, download: op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase), visibleOn: op.VisibleOn);
        }
    }
}
