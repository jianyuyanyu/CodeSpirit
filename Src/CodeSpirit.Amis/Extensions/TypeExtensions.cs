using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Extensions
{
    /// <summary>
    /// 类型检查扩展方法
    /// </summary>
    internal static class TypeExtensions
    {
        public static bool IsEnumType(this Type type) =>
            type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true;

        public static bool IsDateType(this Type type) =>
            type == typeof(DateTime) || type == typeof(DateTime?) ||
            type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);

        public static bool IsNumericType(this Type type) =>
            Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or
                TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
                _ => false
            };

        public static bool IsImageType(this Type type) =>
            type == typeof(string) && type.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.ImageUrl;

        public static T GetAttribute<T>(this ICustomAttributeProvider provider) where T : Attribute
        {
            return (T)provider.GetCustomAttributes(typeof(T), true).FirstOrDefault();
        }

        public static Type GetMemberType(this ICustomAttributeProvider member)
        {
            return member switch
            {
                ParameterInfo p => p.ParameterType,
                PropertyInfo prop => prop.PropertyType,
                _ => throw new NotSupportedException()
            };
        }

        public static string GetMemberName(this ICustomAttributeProvider member)
        {
            return member switch
            {
                ParameterInfo p => p.Name,
                PropertyInfo prop => prop.Name,
                _ => string.Empty
            };
        }

        /// <summary>
        /// 根据类型判断是否必填
        /// </summary>
        public static bool IsTypeRequired(this Type type) =>
            !type.IsNullable();

        /// <summary>
        /// 判断参数或属性是否为可空类型。
        /// </summary>
        /// <param name="type">类型信息。</param>
        /// <returns>如果是可空类型则返回 true，否则返回 false。</returns>
        public static bool IsNullable(this Type type)
        {
            if (!type.IsValueType)
                return true;

            if (Nullable.GetUnderlyingType(type) != null)
                return true;

            return false;
        }

        /// <summary>
        /// 获取枚举类型的选项列表，用于 AMIS 的下拉选择框。
        /// </summary>
        /// <param name="type">枚举类型或可空枚举类型。</param>
        /// <returns>AMIS 枚举选项的 JSON 数组。</returns>
        public static JArray GetEnumOptions(this Type type)
        {
            Type enumType = Nullable.GetUnderlyingType(type) ?? type;
            IEnumerable<object> enumValues = Enum.GetValues(enumType).Cast<object>();
            IEnumerable<JObject> enumOptions = enumValues.Select(e => new JObject
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
        public static string GetEnumDisplayName(this Type enumType, object value)
        {
            string name = Enum.GetName(enumType, value);
            if (name == null)
                return value.ToString();

            MemberInfo member = enumType.GetMember(name).FirstOrDefault();
            if (member == null)
                return name;

            DisplayAttribute displayNameAttr = member.GetCustomAttribute<DisplayAttribute>();
            return displayNameAttr?.Name ?? name;
        }
    }
}
