using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS InputText 字段工厂，用于创建支持附加组件的 input-text 类型的表单字段。
    /// </summary>
    public class AmisInputTextFieldFactory : AmisFieldAttributeFactoryBase
    {
        private readonly AiFormFieldEnhancer _aiEnhancer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="aiEnhancer">AI表单增强器</param>
        public AmisInputTextFieldFactory(AiFormFieldEnhancer aiEnhancer)
        {
            _aiEnhancer = aiEnhancer;
        }
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisInputTextFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建 AMIS input-text 字段配置，支持附加组件
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）</param>
        /// <param name="utilityHelper">实用工具类</param>
        /// <returns>AMIS 字段的 JSON 对象</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisInputTextFieldAttribute attr) = CreateField<AmisInputTextFieldAttribute>(member, utilityHelper);
            if (field != null && attr != null)
            {
                // 如果启用了附加组件，添加附加组件配置
                if (attr.EnableAddOn && !string.IsNullOrEmpty(attr.AddOnLabel))
                {
                    var addOn = new JObject
                    {
                        ["type"] = attr.AddOnType,
                        ["label"] = attr.AddOnLabel,
                        ["level"] = attr.AddOnLevel  // 设置按钮样式等级
                    };

                    // 添加图标
                    if (!string.IsNullOrEmpty(attr.AddOnIcon))
                    {
                        addOn["icon"] = attr.AddOnIcon;
                    }

                    // 添加CSS类名
                    if (!string.IsNullOrEmpty(attr.AddOnClassName))
                    {
                        addOn["className"] = attr.AddOnClassName;
                    }

                    // 添加按钮尺寸
                    if (!string.IsNullOrEmpty(attr.AddOnSize))
                    {
                        addOn["size"] = attr.AddOnSize;
                    }

                    // 添加禁用状态
                    if (attr.AddOnDisabled)
                    {
                        addOn["disabled"] = attr.AddOnDisabled;
                    }

                    // 添加显示条件
                    if (!string.IsNullOrEmpty(attr.AddOnVisibleOn))
                    {
                        addOn["visibleOn"] = attr.AddOnVisibleOn;
                    }

                    // 添加禁用条件
                    if (!string.IsNullOrEmpty(attr.AddOnDisabledOn))
                    {
                        addOn["disabledOn"] = attr.AddOnDisabledOn;
                    }

                    // 添加加载状态文本
                    if (!string.IsNullOrEmpty(attr.AddOnLoadingText))
                    {
                        addOn["loadingText"] = attr.AddOnLoadingText;
                    }

                    // 配置动作
                    if (!string.IsNullOrEmpty(attr.AddOnApi))
                    {
                        // 直接在按钮上配置动作类型
                        addOn["actionType"] = attr.AddOnActionType;

                        // 配置API调用
                        addOn["api"] = new JObject
                        {
                            ["method"] = "post",
                            ["url"] = attr.AddOnApi,
                            ["data"] = new JObject
                            {
                                ["&"] = "$$"  // 传递整个表单数据
                            },
                            ["responseData"] = new JObject
                            {
                                ["&"] = "$$"  // 将API返回的数据合并到表单中
                            }
                        };

                        // 添加确认信息
                        if (!string.IsNullOrEmpty(attr.AddOnConfirmText))
                        {
                            addOn["confirmText"] = attr.AddOnConfirmText;
                        }
                    }

                    // 将附加组件添加到字段配置中
                    field["addOn"] = addOn;
                }
                else
                {
                    // 如果没有手动配置addOn，尝试自动添加AI填充功能
                    // 需要从上下文获取DTO类型信息
                    var dtoType = GetDtoTypeFromContext(member);
                    if (dtoType != null && dtoType != typeof(object))
                    {
                        field = _aiEnhancer.EnhanceField(field, member, dtoType);
                    }
                }
            }

            return field;
        }

        /// <summary>
        /// 从上下文获取DTO类型
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>DTO类型</returns>
        private Type GetDtoTypeFromContext(ICustomAttributeProvider member)
        {
            // 如果是属性，获取其声明类型
            if (member is PropertyInfo property)
            {
                return property.DeclaringType;
            }

            // 如果是参数，尝试获取其声明方法的类型
            if (member is ParameterInfo parameter)
            {
                return parameter.Member?.DeclaringType;
            }

            return typeof(object); // 返回默认类型而不是null
        }
    }
}
