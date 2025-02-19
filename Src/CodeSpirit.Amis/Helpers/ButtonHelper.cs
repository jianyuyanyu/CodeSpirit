using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly AmisContext amisContext;
        private readonly ApiRouteHelper apiRouteHelper;
        private readonly AmisApiHelper amisApiHelper;
        private readonly FormFieldHelper formFieldHelper;

        public ButtonHelper(IHasPermissionService permissionService, AmisContext amisContext, ApiRouteHelper apiRouteHelper, AmisApiHelper amisApiHelper, FormFieldHelper formFieldHelper)
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

        // 创建"新增"按钮
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

        // 创建"编辑"按钮
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

        public JObject CreateDetailButton(ApiRouteInfo detailRoute, IEnumerable<PropertyInfo> detailPropertites)
        {
            string title = "查看";
            JArray controls = [];

            List<JObject> formFields = formFieldHelper.GetAmisFormFieldsFromProperties(detailPropertites);

            // 遍历字段,在每个字段后面添加分割线(最后一个字段除外)
            for (int i = 0; i < formFields.Count(); i++)
            {
                JObject field = formFields[i];

                // 检查是否为图片或头像类型
                if (field["type"]?.ToString() == "image" || field["type"]?.ToString() == "avatar")
                {
                    // 创建control包裹
                    JObject controlWrapper = new()
                    {
                        ["type"] = "control",
                        ["label"] = field["label"],
                        ["body"] = new JArray { field }
                    };
                    // 移除原始的label，因为已经移到control层级
                    field.Remove("label");
                    controls.Add(controlWrapper);
                }
                else
                {
                    controls.Add(field);
                }

                // 如果不是最后一个字段,添加分割线
                if (i < formFields.Count() - 1)
                {
                    controls.Add(new JObject
                    {
                        ["type"] = "divider"
                    });
                }
            }

            JObject drawerBody = new()
            {
                ["title"] = title,
                ["size"] = "lg",
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = detailRoute.ApiPath,
                        ["method"] = detailRoute.HttpMethod
                    },
                    ["controls"] = controls,
                    ["mode"] = "horizontal",
                    ["horizontal"] = new JObject
                    {
                        ["left"] = 3,
                        ["right"] = 9
                    },
                    ["static"] = true,
                    ["submitText"] = "",
                    ["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "关闭",
                            ["actionType"] = "close"
                        }
                    }
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        // 创建"删除"按钮
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
        public List<JObject> GetCustomOperationsButtons(bool isBulkOperation = false)
        {
            List<JObject> buttons = [];
            // 获取当前类型的所有方法
            MethodInfo[] methods = amisContext.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            // 查找带有 [Operation] 特性的所有方法
            foreach (MethodInfo method in methods)
            {
                OperationAttribute op = method.GetCustomAttribute<OperationAttribute>();
                if (op != null && op.IsBulkOperation == isBulkOperation)
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

        // 获取批量操作按钮
        public List<JObject> GetBulkOperationButtons()
        {
            return GetCustomOperationsButtons(true);
        }

        // 创建自定义操作按钮
        public JObject CreateCustomOperationButton(OperationAttribute op, MethodInfo method)
        {
            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = op.Label,
                ["actionType"] = op.ActionType
            };

            // 处理不同的操作类型
            if (op.ActionType == "link")
            {
                // 对于链接类型，使用 link 属性而不是 api
                string url = op.Api ?? apiRouteHelper.GetApiRouteInfoForMethod(method).ApiPath;
                button["link"] = url;
            }
            else if (op.ActionType == "ajax")
            {
                // 对于 ajax 类型，使用 api 属性
                JObject api = amisApiHelper.CreateApiForMethod(method);
                if (api["url"] == null)
                {
                    api["url"] = op.Api;
                }

                if (op.IsBulkOperation)
                {
                    api["data"] = new JObject()
                    {
                        ["ids"] = "${ids|split}"
                    };
                }

                button["api"] = api;
            }
            else if (op.ActionType == "service")
            {
                // 对于 service 类型，创建一个 service 弹窗
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                return CreateServiceDialogButton(op.Label, route);
            }

            // 添加其他通用配置
            if (!string.IsNullOrEmpty(op.ConfirmText))
            {
                button["confirmText"] = op.ConfirmText;
            }

            if (op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                button["download"] = true;
            }

            if (!string.IsNullOrEmpty(op.VisibleOn))
            {
                button["visibleOn"] = op.VisibleOn;
            }

            return button;
        }

        /// <summary>
        /// 创建一个Service弹窗按钮
        /// </summary>
        /// <param name="title">按钮和弹窗标题</param>
        /// <param name="route">API路由信息</param>
        /// <returns>按钮配置对象</returns>
        public JObject CreateServiceDialogButton(string title, ApiRouteInfo route)
        {
            ArgumentNullException.ThrowIfNull(route);
            
            JObject serviceBody = new()
            {
                ["title"] = title,
                ["size"] = "lg",
                ["body"] = new JObject
                {
                    ["type"] = "service",
                    ["schemaApi"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["body"] = "${body}" // 使用Service返回的body内容
                }
            };

            return CreateButton(title, "dialog", dialogOrDrawer: serviceBody);
        }
    }
}
