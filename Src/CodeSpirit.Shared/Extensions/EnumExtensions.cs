﻿using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Shared.Extensions.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举信息(枚举名称、描述、值)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDesc(this Enum value)
        {
            Type type = value.GetType();
            List<string> names = Enum.GetNames(type).ToList();

            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo item in fields)
            {
                if (!names.Contains(item.Name))
                {
                    continue;
                }
                if (value.ToString() != item.Name)
                {
                    continue;
                }
                DescriptionAttribute[] EnumAttributes = (DescriptionAttribute[])item.
            GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (EnumAttributes.Length > 0)
                {
                    return EnumAttributes[0].Description;
                }
                else
                {
                    return "";
                }
            }

            return "";
        }
    }
}
