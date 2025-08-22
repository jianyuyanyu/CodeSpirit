using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Extensions;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    /// <summary>
    /// 卡片模式助手类，负责生成AMIS卡片模式配置
    /// </summary>
    public class CardHelper
    {
        private readonly AmisContext _amisContext;
        private readonly ButtonHelper _buttonHelper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="amisContext">AMIS上下文</param>
        /// <param name="buttonHelper">按钮助手</param>
        public CardHelper(AmisContext amisContext, ButtonHelper buttonHelper)
        {
            _amisContext = amisContext;
            _buttonHelper = buttonHelper;
        }

        /// <summary>
        /// 检查控制器是否支持卡片模式
        /// </summary>
        /// <param name="controllerType">控制器类型</param>
        /// <returns>如果支持卡片模式返回true，否则返回false</returns>
        public bool IsCardModeSupported(Type controllerType)
        {
            return controllerType.GetCustomAttribute<AmisCardAttribute>() != null;
        }

        /// <summary>
        /// 生成卡片配置
        /// </summary>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>卡片配置对象</returns>
        public JObject GenerateCardConfig(Type controllerType, Type dataType)
        {
            var cardAttribute = controllerType.GetCustomAttribute<AmisCardAttribute>();
            if (cardAttribute == null)
            {
                return null;
            }

            var cardConfig = new JObject();

            // 生成卡片头部配置
            var headerConfig = GenerateCardHeader(cardAttribute, dataType);
            if (headerConfig != null)
            {
                cardConfig["header"] = headerConfig;
            }

            // 生成卡片主体配置
            var bodyTemplate = GenerateCardBodyTemplate(cardAttribute, dataType);
            if (!string.IsNullOrEmpty(bodyTemplate))
            {
                cardConfig["body"] = bodyTemplate;
            }

            // 设置主体样式
            if (!string.IsNullOrEmpty(cardAttribute.BodyClassName))
            {
                cardConfig["bodyClassName"] = cardAttribute.BodyClassName;
            }

            // 生成卡片操作按钮
            var actions = GenerateCardActions();
            if (actions != null && actions.Any())
            {
                cardConfig["actions"] = new JArray(actions);
            }

            return cardConfig;
        }

        /// <summary>
        /// 生成卡片头部配置
        /// </summary>
        /// <param name="cardAttribute">卡片特性</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>头部配置对象</returns>
        private JObject GenerateCardHeader(AmisCardAttribute cardAttribute, Type dataType)
        {
            var headerConfig = new JObject();

            // 设置头部样式
            if (!string.IsNullOrEmpty(cardAttribute.HeaderClassName))
            {
                headerConfig["className"] = cardAttribute.HeaderClassName;
            }

            // 获取属性配置
            var properties = dataType.GetProperties();
            foreach (var prop in properties)
            {
                var cardFieldAttr = prop.GetCustomAttribute<AmisCardFieldAttribute>();
                var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                var propName = prop.Name.ToCamelCase();

                if (cardFieldAttr != null)
                {
                    switch (cardFieldAttr.FieldType)
                    {
                        case CardFieldType.Title:
                            headerConfig["title"] = $"${{TRUNCATE({propName},30)}}";
                            break;
                        case CardFieldType.SubTitle:
                            headerConfig["subTitle"] = $"${{{propName}}}";
                            break;
                        case CardFieldType.Description:
                            headerConfig["description"] = $"${{{propName}}}";
                            break;
                        case CardFieldType.Avatar:
                            headerConfig["avatar"] = $"${{{propName}}}";
                            if (!string.IsNullOrEmpty(cardAttribute.AvatarClassName))
                            {
                                headerConfig["avatarClassName"] = cardAttribute.AvatarClassName;
                            }
                            break;
                        case CardFieldType.Highlight:
                            headerConfig["highlight"] = $"${{{propName}}}";
                            break;
                    }
                }
            }

            // 使用配置的字段
            if (!string.IsNullOrEmpty(cardAttribute.TitleField))
            {
                string titleField = cardAttribute.TitleField.ToCamelCase();
                headerConfig["title"] = $"${{TRUNCATE({titleField},25)}}";
            }
            if (!string.IsNullOrEmpty(cardAttribute.SubTitleField))
            {
                string subTitleField = cardAttribute.SubTitleField.ToCamelCase();
                headerConfig["subTitle"] = $"${{{subTitleField}}}";
            }
            if (!string.IsNullOrEmpty(cardAttribute.DescriptionField))
            {
                string descriptionField = cardAttribute.DescriptionField.ToCamelCase();
                headerConfig["description"] = $"${{{descriptionField}}}";
            }
            if (!string.IsNullOrEmpty(cardAttribute.AvatarField))
            {
                string avatarField = cardAttribute.AvatarField.ToCamelCase();
                headerConfig["avatar"] = $"${{{avatarField} | raw}}";
                if (!string.IsNullOrEmpty(cardAttribute.AvatarClassName))
                {
                    headerConfig["avatarClassName"] = cardAttribute.AvatarClassName;
                }
            }
            if (!string.IsNullOrEmpty(cardAttribute.HighlightField))
            {
                string highlightField = cardAttribute.HighlightField.ToCamelCase();
                headerConfig["highlight"] = $"${{{highlightField}}}";
            }

            return headerConfig.Count > 0 ? headerConfig : null;
        }

        /// <summary>
        /// 生成卡片主体内容模板
        /// </summary>
        /// <param name="cardAttribute">卡片特性</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>主体内容模板字符串</returns>
        private string GenerateCardBodyTemplate(AmisCardAttribute cardAttribute, Type dataType)
        {
            // 如果已配置模板，直接使用
            if (!string.IsNullOrEmpty(cardAttribute.BodyTemplate))
            {
                return cardAttribute.BodyTemplate;
            }

            // 自动生成模板
            var bodyFields = new List<string>();
            var properties = dataType.GetProperties()
                .Where(p => p.GetCustomAttribute<AmisCardFieldAttribute>()?.FieldType == CardFieldType.Body)
                .OrderBy(p => p.GetCustomAttribute<AmisCardFieldAttribute>()?.Order ?? 0);

            foreach (var prop in properties)
            {
                var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                var fieldName = displayNameAttr?.DisplayName ?? prop.Name;
                var cardFieldAttr = prop.GetCustomAttribute<AmisCardFieldAttribute>();

                if (!string.IsNullOrEmpty(cardFieldAttr?.Template))
                {
                    bodyFields.Add(cardFieldAttr.Template);
                }
                else
                {
                    string propNameLower = char.ToLower(prop.Name[0]) + prop.Name[1..];
                    bodyFields.Add($"<p><strong>{fieldName}:</strong> ${{this.{propNameLower}}}</p>");
                }
            }

            if (bodyFields.Any())
            {
                return string.Join("\n", bodyFields);
            }

            // 默认模板
            return "<p class=\"text-muted\">暂无详细信息</p>";
        }

        /// <summary>
        /// 生成卡片操作按钮
        /// </summary>
        /// <returns>操作按钮数组</returns>
        private List<JObject> GenerateCardActions()
        {
            var actions = new List<JObject>();

            // 添加编辑按钮
            if (_amisContext.ApiRoutes.Update != null && _amisContext.Actions.Update != null)
            {
                var editButton = _buttonHelper.CreateEditButton(_amisContext.ApiRoutes.Update, _amisContext.Actions.Update.GetParameters());
                if (editButton != null)
                {
                    actions.Add(editButton);
                }
            }

            // 添加删除按钮
            if (_amisContext.ApiRoutes.Delete != null && _amisContext.Actions.Delete != null)
            {
                var deleteButton = _buttonHelper.CreateDeleteButton(_amisContext.ApiRoutes.Delete);
                if (deleteButton != null)
                {
                    actions.Add(deleteButton);
                }
            }

            // 添加其他操作按钮
            var operationButtons = _buttonHelper.GetOperationButtons();
            if (operationButtons != null)
            {
                actions.AddRange(operationButtons);
            }

            return actions;
        }

        /// <summary>
        /// 获取卡片模式的默认配置
        /// </summary>
        /// <param name="cardAttribute">卡片特性</param>
        /// <returns>默认配置对象</returns>
        public JObject GetCardModeDefaultParams(AmisCardAttribute cardAttribute)
        {
            return new JObject
            {
                ["perPage"] = cardAttribute.DefaultPerPage
            };
        }
    }
}
