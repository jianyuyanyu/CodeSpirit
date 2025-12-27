using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// 增强的批量导入字段工厂类
    /// </summary>
    public class AmisEnhancedImportFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisEnhancedImportFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建增强的批量导入字段
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具助手</param>
        /// <returns>AMIS字段配置</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, attr) = CreateField<AmisEnhancedImportFieldAttribute>(member, utilityHelper);
            if (field == null || attr == null)
                return field;

            // 获取泛型类型信息
            var type = utilityHelper.GetMemberType(member);
            var itemType = type.GenericTypeArguments.FirstOrDefault();

            if (itemType == null)
                return field;

            // 使用Service组件加载JSONP配置，通过数据链传递参数
            var serviceWrapper = new JObject
            {
                ["type"] = "service",
                ["name"] = $"enhancedImportService_{field["name"]}",
                ["data"] = new JObject
                {
                    // 通过数据链设置配置参数
                    ["enhancedImportConfig"] = new JObject
                    {
                        ["fieldName"] = field["name"],
                        ["itemType"] = itemType.Name,
                        ["maxLength"] = attr.MaxLength,
                        ["showTemplateDownload"] = attr.ShowTemplateDownload,
                        ["showImportResult"] = attr.ShowImportResult,
                        ["templateDownloadText"] = attr.TemplateDownloadText,
                        ["importButtonText"] = attr.ImportButtonText,
                        ["templateDownloadApi"] = string.IsNullOrEmpty(attr.TemplateDownloadApi) 
                            ? $"${{BASE_API}}/import/template?type={itemType.Name}" 
                            : attr.TemplateDownloadApi,
                        ["submitApi"] = string.IsNullOrEmpty(attr.SubmitApi)
                            ? $"${{BASE_API}}/batch/import"
                            : attr.SubmitApi,
                        ["label"] = field["label"],
                        ["required"] = field["required"],
                        ["createInputTable"] = attr.CreateInputTable,
                        ["columns"] = attr.CreateInputTable ? ExtractPropertyInfo(itemType, utilityHelper) : new JArray()
                    }
                },
                ["schemaApi"] = $"js:/amis/configs/common/enhanced-import.js?type={itemType.Name}"
            };

            return serviceWrapper;
        }

        /// <summary>
        /// 提取属性信息生成表格列配置
        /// </summary>
        /// <param name="targetType">目标类型</param>
        /// <param name="utilityHelper">工具助手（用于获取当前语言）</param>
        /// <returns>列配置数组</returns>
        public JArray ExtractPropertyInfo(Type targetType, UtilityHelper utilityHelper)
        {
            if (targetType == null) return new JArray();

            var fieldArray = new JArray();
            var properties = targetType.GetProperties();

            foreach (var property in properties)
            {
                // 获取字段名称
                var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
                string fieldName = jsonProperty?.PropertyName ?? property.Name;

                // 获取显示名称（支持多语言资源）
                var displayName = property.GetDisplayName(utilityHelper);

                // 创建表单字段
                var field = property.CreateFormField(fieldName: fieldName, lableName: displayName);

                // 添加必填验证
                var required = property.GetCustomAttribute<RequiredAttribute>();
                if (required != null)
                {
                    field["required"] = true;
                    field["validations"] = new JObject
                    {
                        ["isRequired"] = true
                    };
                    field["validationErrors"] = new JObject
                    {
                        ["isRequired"] = required.ErrorMessage ?? $"{displayName}不能为空"
                    };
                }

                fieldArray.Add(field);
            }

            return fieldArray;
        }

        /// <summary>
        /// 创建导入结果展示区域
        /// </summary>
        /// <returns>结果展示配置</returns>
        private JObject CreateImportResultBody()
        {
            return new JObject
            {
                ["type"] = "panel",
                ["title"] = "导入结果",
                ["className"] = "import-result-panel",
                ["body"] = new JArray
                {
                    // 成功统计
                    new JObject
                    {
                        ["type"] = "alert",
                        ["level"] = "success",
                        ["body"] = "成功导入 ${successCount} 条记录",
                        ["visibleOn"] = "${successCount > 0}"
                    },
                    
                    // 失败统计
                    new JObject
                    {
                        ["type"] = "alert",
                        ["level"] = "danger",
                        ["body"] = "导入失败 ${failedCount} 条记录",
                        ["visibleOn"] = "${failedCount > 0}"
                    },

                    // 失败记录详情CRUD
                    new JObject
                    {
                        ["type"] = "crud",
                        ["title"] = "失败记录详情",
                        ["source"] = "${failedRecords}",
                        ["visibleOn"] = "${failedRecords && failedRecords.length > 0}",
                        ["syncLocation"] = false,
                        ["perPage"] = 1000,
                        ["columns"] = new JArray
                        {
                            new JObject
                            {
                                ["name"] = "rowIndex",
                                ["label"] = "行号",
                                ["type"] = "text"
                            },
                            new JObject
                            {
                                ["name"] = "errorMessage",
                                ["label"] = "错误信息",
                                ["type"] = "text"
                            }
                        },
                        ["headerToolbar"] = new JArray
                        {
                            "export-excel"
                        }
                    }
                }
            };
        }
    }
}
