using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class UtilityHelper
    {
        public string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length == 1)
                return str.ToLower();

            return char.ToLower(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// 判断参数或属性是否为可空类型。
        /// </summary>
        /// <param name="type">类型信息。</param>
        /// <returns>如果是可空类型则返回 true，否则返回 false。</returns>
        public bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true;

            if (Nullable.GetUnderlyingType(type) != null)
                return true;

            return false;
        }

        /// <summary>
        /// 判断参数或属性是否为简单类型（如基本数据类型、字符串、枚举等）。
        /// </summary>
        /// <param name="type">参数或属性的类型。</param>
        /// <returns>如果是简单类型则返回 true，否则返回 false。</returns>
        public bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || new Type[]
                {
                    typeof(string), typeof(decimal), typeof(DateTime),
                    typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
                }.Contains(type)
                || type.IsEnum
                || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)));
        }

        /// <summary>
        /// 判断类型是否为复杂类型（如类类型，不包括字符串）。
        /// </summary>
        /// <param name="type">参数或属性的类型。</param>
        /// <returns>如果是复杂类型则返回 true，否则返回 false。</returns>
        public bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        /// <summary>
        /// 获取枚举类型的选项列表，用于 AMIS 的下拉选择框。
        /// </summary>
        /// <param name="type">枚举类型或可空枚举类型。</param>
        /// <returns>AMIS 枚举选项的 JSON 数组。</returns>
        public JArray GetEnumOptions(Type type)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            var enumValues = Enum.GetValues(enumType).Cast<object>();
            var enumOptions = enumValues.Select(e => new JObject
            {
                ["label"] = GetEnumDisplayName(enumType, e),
                ["value"] = e.ToString()
            });

            return new JArray(enumOptions);
        }

        /// <summary>
        /// 获取枚举成员的显示名称。优先从 <see cref="DisplayNameAttribute"/> 获取，否则使用枚举成员的名称。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <param name="value">枚举值。</param>
        /// <returns>枚举成员的显示名称。</returns>
        public string GetEnumDisplayName(Type enumType, object value)
        {
            var name = Enum.GetName(enumType, value);
            if (name == null)
                return value.ToString();

            var member = enumType.GetMember(name).FirstOrDefault();
            if (member == null)
                return name;

            var displayNameAttr = member.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttr?.DisplayName ?? name;
        }

        /// <summary>
        /// 获取成员的类型（PropertyInfo 或 ParameterInfo）。
        /// </summary>
        /// <param name="member">成员信息。</param>
        /// <returns>成员的类型。</returns>
        public Type GetMemberType(ICustomAttributeProvider member)
        {
            return member switch
            {
                PropertyInfo prop => prop.PropertyType,
                ParameterInfo param => param.ParameterType,
                _ => throw new ArgumentException("Unsupported member type.")
            };
        }
    }
}

