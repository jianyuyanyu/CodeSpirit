using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 时间字段工厂类
    /// </summary>
    public class AmisTimeFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisTimeFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建时间字段配置
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具辅助类</param>
        /// <returns>字段配置</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisTimeFieldAttribute attr) = CreateField<AmisTimeFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                // 设置显示格式
                field["format"] = attr.DisplayFormat;
                
                // 设置最小值
                if (!string.IsNullOrEmpty(attr.Min))
                {
                    field["minTime"] = attr.Min;
                }
                
                // 设置最大值
                if (!string.IsNullOrEmpty(attr.Max))
                {
                    field["maxTime"] = attr.Max;
                }
                
                // 设置占位符
                field["placeholder"] = attr.InputPlaceholder;
                
                // 设置是否可清除
                field["clearable"] = attr.Clearable;
                
                
                // 设置是否使用当前时间
                if (attr.UseCurrentTime)
                {
                    field["value"] = "${NOW}";
                }
                
                // 设置时间分隔符
                field["timeFormat"] = attr.TimeSeparator;
                
                // 是否显示秒选择
                field["showSeconds"] = attr.ShowSeconds;

                // 设置步长
                field["timeStep"] = attr.Step;
                
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