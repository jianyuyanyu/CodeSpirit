using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 日期时间字段工厂类
    /// </summary>
    public class AmisDatetimeFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisDatetimeFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建日期时间字段配置
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具辅助类</param>
        /// <returns>字段配置</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisDatetimeFieldAttribute attr) = CreateField<AmisDatetimeFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                // 设置显示格式
                field["format"] = attr.DisplayFormat;
                
                // 设置选择器格式（如果有）
                if (!string.IsNullOrEmpty(attr.PickerFormat))
                {
                    field["inputFormat"] = attr.PickerFormat;
                }
                
                // 设置最小值
                if (!string.IsNullOrEmpty(attr.Min))
                {
                    field["minDate"] = attr.Min;
                }
                
                // 设置最大值
                if (!string.IsNullOrEmpty(attr.Max))
                {
                    field["maxDate"] = attr.Max;
                }
                
                // 设置占位符
                field["placeholder"] = attr.InputPlaceholder;
                
                // 设置是否可清除
                field["clearable"] = attr.Clearable;
                
                // 设置是否只读
                field["readOnly"] = attr.ReadOnly;
                
                // 设置分隔符
                field["dateSeparator"] = attr.DateSeparator;
                field["timeFormat"] = attr.TimeSeparator;
                
                // 设置是否显示清除按钮
                field["clearable"] = attr.ShowClearBtn;
                
                // 设置是否显示图标
                field["icon"] = attr.ShowIcon;

                // 设置UTC时间选项
                if (attr.Utc)
                {
                    field["utc"] = attr.Utc;
                }

                // 设置时区偏移
                if (!string.IsNullOrEmpty(attr.TimeZone))
                {
                    field["timeZone"] = attr.TimeZone;
                }
            }
            return field;
        }
        /// <summary>
        /// 处理日期时间字段的默认值
        /// </summary>
        /// <param name="field">AMIS字段配置</param>
        /// <param name="attr">字段特性</param>
        protected override void HandleDefaultValue(JObject field, AmisFormFieldAttribute attr)
        {
            var datetimeAttr = attr as AmisDatetimeFieldAttribute;
            if (datetimeAttr == null)
            {
                base.HandleDefaultValue(field, attr);
                return;
            }

            // 处理相对时间表达式
            if (!string.IsNullOrEmpty(datetimeAttr.RelativeTime))
            {
                string expression = datetimeAttr.RelativeTime.ToLower() switch
                {
                    "today" => "${now}",
                    "yesterday" => "${now | dateModify -1 days}",
                    "tomorrow" => "${now | dateModify 1 days}",
                    "lastweek" => "${now | dateModify -7 days}",
                    "nextweek" => "${now | dateModify 7 days}",
                    "lastmonth" => "${now | dateModify -1 month}",
                    "nextmonth" => "${now | dateModify 1 month}",
                    _ => datetimeAttr.RelativeTime // 如果是自定义表达式，直接使用
                };

                // 处理时间偏移
                if (datetimeAttr.TimeOffset != 0)
                {
                    expression = $"{expression} | dateModify {datetimeAttr.TimeOffset} minutes";
                }

                field["value"] = expression;
                return;
            }

            // 处理旧的 UseCurrentTime 属性（向后兼容）
#pragma warning disable CS0618 // 禁用过时警告
            if (datetimeAttr.UseCurrentTime)
            {
                string expression = "${now}";
                if (datetimeAttr.TimeOffset != 0)
                {
                    expression = $"{expression} | dateModify {datetimeAttr.TimeOffset} minutes";
                }
                field["value"] = expression;
                return;
            }
#pragma warning restore CS0618

            // 处理其他默认值情况
            if (attr.ValueType == DefaultValueType.CurrentDateTime)
            {
                string expression = "${now}";
                if (datetimeAttr.TimeOffset != 0)
                {
                    expression = $"{expression} | dateModify {datetimeAttr.TimeOffset} minutes";
                }
                field["value"] = expression;
                return;
            }

            // 如果没有特殊处理，使用基类的默认值处理
            base.HandleDefaultValue(field, attr);
        }
    }
}