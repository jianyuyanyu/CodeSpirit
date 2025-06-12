using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 日期字段工厂类
    /// </summary>
    public class AmisDateFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisDateFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建日期字段配置
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具辅助类</param>
        /// <returns>字段配置</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisDateFieldAttribute attr) = CreateField<AmisDateFieldAttribute>(member, utilityHelper);
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
                
                // 设置是否使用当前日期
                if (attr.UseCurrentDate)
                {
                    field["value"] = "${NOW}";
                }

                // 设置分隔符
                field["dateSeparator"] = attr.DateSeparator;
                
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
    }
} 