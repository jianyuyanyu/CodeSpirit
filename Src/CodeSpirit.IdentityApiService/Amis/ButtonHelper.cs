using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly PermissionService _permissionService;
        private readonly Type _dataType;
        private readonly string _controllerName;

        public ButtonHelper(PermissionService permissionService, Type dataType, string controllerName)
        {
            _permissionService = permissionService;
            _dataType = dataType;
            _controllerName = controllerName;
        }

        public JObject CreateHeaderButton(string createRoute, IEnumerable<ParameterInfo> createParameters)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "新增",
                ["level"] = "primary",
                ["actionType"] = "dialog",
                ["dialog"] = new JObject
                {
                    ["title"] = "新增",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["api"] = new JObject
                        {
                            ["url"] = createRoute,
                            ["method"] = "post"
                        },
                        ["body"] = new JArray(new FormFieldHelper(_permissionService, new UtilityHelper()).GetAmisFormFieldsFromParameters(createParameters))
                    }
                }
            };
        }

        public JObject CreateEditButton(string updateRoute)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "编辑",
                ["actionType"] = "drawer",
                ["drawer"] = new JObject
                {
                    ["title"] = "编辑",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["api"] = new JObject
                        {
                            ["url"] = updateRoute,
                            ["method"] = "put"
                        },
                        ["body"] = new JArray(new FormFieldHelper(_permissionService, new UtilityHelper()).GetAmisFormFieldsFromParameters(null)) // Pass actual parameters if needed
                    }
                }
            };
        }

        public JObject CreateDeleteButton(string deleteRoute)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "删除",
                ["actionType"] = "ajax",
                ["confirmText"] = "确定要删除吗？",
                ["api"] = new JObject
                {
                    ["url"] = deleteRoute,
                    ["method"] = "delete"
                }
            };
        }

        public List<JObject> GetCustomOperationsButtons()
        {
            var buttons = new List<JObject>();
            // 实现自定义按钮逻辑，可能通过反射查找带有 [Operation] 特性的操作方法

            // 示例：假设有自定义操作
            // 可以通过注入其他依赖或使用反射动态生成
            return buttons;
        }

        public JObject CreateCustomOperationButton(OperationAttribute op)
        {
            var button = new JObject
            {
                ["type"] = "button",
                ["label"] = op.Label,
                ["actionType"] = op.ActionType
            };

            if (!string.IsNullOrEmpty(op.Api))
            {
                button["api"] = new JObject
                {
                    ["url"] = op.Api,
                    ["method"] = op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase) ? "get" : "post"
                };
            }

            if (!string.IsNullOrEmpty(op.ConfirmText))
            {
                button["confirmText"] = op.ConfirmText;
            }

            if (op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                button["download"] = true;
            }

            return button;
        }
    }
}

