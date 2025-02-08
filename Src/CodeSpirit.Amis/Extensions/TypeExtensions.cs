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
    }
}
