using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Attributes;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AI表单字段增强器
    /// </summary>
    public class AiFormFieldEnhancer
    {
        private readonly UtilityHelper _utilityHelper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="utilityHelper">实用工具类</param>
        public AiFormFieldEnhancer(UtilityHelper utilityHelper)
        {
            _utilityHelper = utilityHelper;
        }

        /// <summary>
        /// 增强字段配置，自动添加AI填充功能
        /// </summary>
        /// <param name="field">字段配置</param>
        /// <param name="member">成员信息</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>增强后的字段配置</returns>
        public JObject EnhanceField(JObject field, ICustomAttributeProvider member, Type dtoType)
        {
            if (field == null || dtoType == null) return field;

            // 检查DTO是否标记了AI填充特性
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
            if (aiFormFillAttr == null) return field;

            var fieldName = field["name"]?.ToString();
            if (string.IsNullOrEmpty(fieldName) || !fieldName.Equals(aiFormFillAttr.TriggerField, StringComparison.CurrentCultureIgnoreCase))
                return field;

            // 只对文本输入字段添加AI功能
            var fieldType = field["type"]?.ToString();
            if (fieldType != "input-text") return field;

            // 检查是否已经配置了addOn，避免覆盖现有配置
            if (field["addOn"] != null) return field;

            // 自动添加AI填充按钮
            var apiEndpoint = string.IsNullOrEmpty(aiFormFillAttr.ApiEndpoint)
                ? "/api/ai-form-fill/ai-fill"
                : aiFormFillAttr.ApiEndpoint;

            field["addOn"] = new JObject
            {
                ["type"] = "button",
                ["label"] = " ",
                ["icon"] = "fa fa-magic", // 魔法棒图标
                ["level"] = "info",
                ["actionType"] = "ajax",
                ["loadingText"] = "AI正在生成中...",
                ["api"] = new JObject
                {
                    ["method"] = "post",
                    ["url"] = apiEndpoint,
                    ["data"] = new JObject
                    {
                        ["&"] = "$$" // 传递整个表单数据
                    },
                    ["responseData"] = new JObject
                    {
                        ["&"] = "$$" // 将API返回的数据合并到表单中
                    }
                }
            };

            return field;
        }
    }


}
