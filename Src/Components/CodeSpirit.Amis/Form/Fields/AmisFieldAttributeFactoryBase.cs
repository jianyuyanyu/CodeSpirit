﻿// 文件路径: CodeSpirit.Amis.Form/AmisFieldAttributeFactoryBase.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Amis.Form.Fields
{
    public abstract class AmisFieldAttributeFactoryBase : IAmisFieldFactory
    {
        /// <summary>
        /// 创建 AMIS 字段配置，基于 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>AMIS 字段的 JSON 对象，如果没有 AmisFieldAttribute 则返回 null。</returns>
        public virtual JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, fieldAttr) = CreateField<AmisFormFieldAttribute>(member, utilityHelper);
            return field;
        }

        public abstract bool CanHandle(Type attributeType);

        /// <summary>
        /// 创建 AMIS 字段配置，基于 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>AMIS 字段的 JSON 对象，如果没有 AmisFieldAttribute 则返回 null。</returns>
        public virtual (JObject, T) CreateField<T>(ICustomAttributeProvider member, UtilityHelper utilityHelper) where T : AmisFormFieldAttribute
        {
            // 使用扩展方法尝试获取 AmisFieldAttribute 及相关信息
            if (!member.TryGetAmisFieldData(utilityHelper, out T fieldAttr, out string displayName, out string fieldName))
                return (null, fieldAttr);

            if (fieldAttr == null) return (null, null);
            
            // 计算是否为必填字段
            // 1. 检查AmisFormFieldAttribute的Required属性
            // 2. 检查DataAnnotations.RequiredAttribute特性
            // 3. 检查字段类型是否为非空类型
            bool isRequired = fieldAttr.Required 
                || HasRequiredAttribute(member) 
                || !utilityHelper.IsNullable(utilityHelper.GetMemberType(member));

            // 创建字段配置
            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = fieldAttr.Label ?? displayName,
                ["required"] = isRequired,
                ["type"] = fieldAttr.Type,
                ["placeholder"] = fieldAttr.Placeholder,
            };

            if(!string.IsNullOrEmpty(fieldAttr.VisibleOn))
                field["visibleOn"] = fieldAttr.VisibleOn;

            if (fieldAttr.Hidden)
            {
                field["hidden"] = fieldAttr.Hidden;
            }

            // 处理默认值
            HandleDefaultValue(field, fieldAttr);

            // 处理通用状态属性
            if (fieldAttr.Disabled)
            {
                field["disabled"] = fieldAttr.Disabled;
            }

            if (fieldAttr.Static)
            {
                field["static"] = fieldAttr.Static;
            }

            if (fieldAttr.SubmitOnChange)
                field["submitOnChange"] = fieldAttr.SubmitOnChange;

            // 处理额外的自定义配置
            utilityHelper.HandleAdditionalConfig(fieldAttr.AdditionalConfig, field);

            return (field, fieldAttr);
        }

        /// <summary>
        /// 检查成员是否包含RequiredAttribute特性
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>是否包含Required特性</returns>
        private bool HasRequiredAttribute(ICustomAttributeProvider member)
        {
            return member switch
            {
                MemberInfo m => m.GetCustomAttribute<RequiredAttribute>() != null,
                ParameterInfo p => p.GetCustomAttribute<RequiredAttribute>() != null,
                _ => false
            };
        }

        /// <summary>
        /// 处理字段的默认值
        /// </summary>
        /// <param name="field">AMIS字段配置</param>
        /// <param name="attr">字段特性</param>
        protected virtual void HandleDefaultValue(JObject field, AmisFormFieldAttribute attr)
        {
            // 按优先级处理默认值
            if (attr.DefaultValue != null)
            {
                field["value"] = JToken.FromObject(attr.DefaultValue);
                return;
            }

            if (!string.IsNullOrEmpty(attr.DefaultValueExpression))
            {
                field["value"] = attr.DefaultValueExpression;
                return;
            }

            // 根据ValueType处理特殊的默认值类型
            switch (attr.ValueType)
            {
                case DefaultValueType.CurrentDateTime:
                    field["value"] = "${now}";
                    break;
                case DefaultValueType.CurrentUser:
                    field["value"] = "${user}";
                    break;
                case DefaultValueType.Expression:
                    // 如果设置了ValueType为Expression但没有设置DefaultValueExpression，使用空表达式
                    if (string.IsNullOrEmpty(attr.DefaultValueExpression))
                    {
                        field["value"] = "${value}";
                    }
                    break;
                case DefaultValueType.Custom:
                    // Custom类型的处理交给具体的字段工厂实现
                    HandleCustomDefaultValue(field, attr);
                    break;
                case DefaultValueType.Static:
                default:
                    // 如果没有设置新的默认值属性，则使用旧的Value属性（向后兼容）
                    if (!string.IsNullOrEmpty(attr.Value))
                    {
                        field["value"] = attr.Value;
                    }
                    break;
            }
        }

        /// <summary>
        /// 处理自定义类型的默认值，可由派生类重写
        /// </summary>
        /// <param name="field">AMIS字段配置</param>
        /// <param name="attr">字段特性</param>
        protected virtual void HandleCustomDefaultValue(JObject field, AmisFormFieldAttribute attr)
        {
            // 基类不做任何处理，由具体的字段工厂实现
        }
    }
}