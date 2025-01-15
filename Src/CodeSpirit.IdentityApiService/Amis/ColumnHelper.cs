using System.ComponentModel;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class ColumnHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;

        public ColumnHelper(PermissionService permissionService, UtilityHelper utilityHelper)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
        }

        public List<JObject> GetAmisColumns(Type dataType, string controllerName, (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute) apiRoutes)
        {
            var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = properties
                .Where(p => !IsIgnoredProperty(p) && HasViewPermission(p))
                .Select(p => CreateAmisColumn(p))
                .ToList();

            var operations = CreateOperationsColumn(controllerName, dataType, apiRoutes.UpdateRoute, apiRoutes.DeleteRoute);
            if (operations != null)
            {
                columns.Add(operations);
            }

            return columns;
        }

        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            return prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)
                || prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasViewPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        private JObject CreateAmisColumn(PropertyInfo prop)
        {
            var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            var fieldName = _utilityHelper.ToCamelCase(prop.Name);

            var column = new JObject
            {
                ["name"] = fieldName,
                ["label"] = displayName,
                ["sortable"] = true,
                ["type"] = GetColumnType(prop)
            };

            if (IsPrimaryKey(prop))
            {
                column["hidden"] = true;
            }

            return column;
        }

        private string GetColumnType(PropertyInfo prop)
        {
            return prop.PropertyType switch
            {
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                _ => "text"
            };
        }

        private bool IsPrimaryKey(PropertyInfo prop)
        {
            return prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        private JObject CreateOperationsColumn(string controllerName, Type dataType, string updateRoute, string deleteRoute)
        {
            var buttons = new JArray();

            // 编辑按钮
            if (_permissionService.HasPermission($"{controllerName}Edit"))
            {
                var editButton = new ButtonHelper(_permissionService, dataType, controllerName).CreateEditButton(updateRoute);
                buttons.Add(editButton);
            }

            // 删除按钮
            if (_permissionService.HasPermission($"{controllerName}Delete"))
            {
                var deleteButton = new ButtonHelper(_permissionService, dataType, controllerName).CreateDeleteButton(deleteRoute);
                buttons.Add(deleteButton);
            }

            // 自定义操作按钮
            var customButtons = new ButtonHelper(_permissionService, dataType, controllerName).GetCustomOperationsButtons();
            foreach (var btn in customButtons)
            {
                buttons.Add(btn);
            }

            if (buttons.Count == 0)
                return null;

            return new JObject
            {
                ["label"] = "操作",
                ["type"] = "operation",
                ["buttons"] = buttons
            };
        }
    }
}

