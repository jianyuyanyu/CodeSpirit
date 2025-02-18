using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CodeSpirit.Core.Extensions;

namespace CodeSpirit.Amis.Helpers
{
    public class UtilityHelper
    {
        public void HandleAdditionalConfig(string additionalConfig, JObject field)
        {
            if (string.IsNullOrEmpty(additionalConfig))
                return;

            try
            {
                JObject additional = JObject.Parse(additionalConfig);
                foreach (JProperty prop in additional.Properties())
                {
                    field[prop.Name] = prop.Value;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
            }
        }

        

        //public bool IsNullable(Type type)
        //{
        //    return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        //}

        public Type GetMemberType(ICustomAttributeProvider member)
        {
            return member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                ParameterInfo param => param.ParameterType,
                _ => throw new NotSupportedException("Member type not supported")
            };
        }

        public bool IsComplexType(Type type)
        {
            return !IsSimpleType(type);
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
                || Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type));
        }

        

        ///// <summary>
        ///// 获取成员的类型（PropertyInfo 或 ParameterInfo）。
        ///// </summary>
        ///// <param name="member">成员信息。</param>
        ///// <returns>成员的类型。</returns>
        //public Type GetMemberType(ICustomAttributeProvider member)
        //{
        //    return member switch
        //    {
        //        PropertyInfo prop => prop.PropertyType,
        //        ParameterInfo param => param.ParameterType,
        //        _ => throw new ArgumentException("Unsupported member type.")
        //    };
        //}

        public List<PropertyInfo> GetOrderedProperties(Type type)
        {
            // 使用 MetadataToken 按声明顺序排序
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .OrderBy(p => p.MetadataToken)
                       .ToList();
        }

        /// <summary>
        /// 判断类型是否为可空枚举类型。
        /// </summary>
        /// <param name="type">需要判断的类型。</param>
        /// <returns>如果是可空枚举类型则返回 true，否则返回 false。</returns>
        public bool IsNullableEnum(Type type)
        {
            Type underlying = Nullable.GetUnderlyingType(type);
            return underlying != null && underlying.IsEnum;
        }

        /// <summary>
        /// 获取类型的基础类型，处理泛型类型（如 ActionResult<> 和 Task<>）。
        /// </summary>
        /// <param name="type">待分析的类型。</param>
        /// <returns>基础类型。</returns>
        public Type GetUnderlyingType(Type type)
        {
            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ActionResult<>))
                {
                    return type.GetGenericArguments()[0];
                }
                if (genericDef == typeof(Task<>))
                {
                    Type taskInnerType = type.GetGenericArguments()[0];
                    if (taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                    {
                        return taskInnerType.GetGenericArguments()[0];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 提取 API 响应的数据类型。
        /// </summary>
        /// <param name="type">待分析的类型。</param>
        /// <returns>提取后的数据类型。</returns>
        public Type ExtractDataType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ApiResponse<>))
                {
                    Type innerType = type.GetGenericArguments()[0];
                    if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(PageList<>))
                    {
                        return innerType.GetGenericArguments()[0];
                    }
                    return innerType;
                }
                if (genericDef == typeof(PageList<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        /// <summary>
        /// 从读取方法中提取返回的数据类型。
        /// </summary>
        /// <param name="method">方法。</param>
        /// <returns>返回的数据类型。</returns>
        public Type GetDataTypeFromMethod(MethodInfo method)
        {
            if (method == null)
                return null;

            Type returnType = method.ReturnType;
            return ExtractDataType(GetUnderlyingType(returnType));
        }

        /// <summary>
        /// 判断属性是否为枚举类型或可空枚举类型。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果是枚举类型则返回 true，否则返回 false。</returns>
        public bool IsEnumProperty(PropertyInfo prop)
        {
            return prop.PropertyType.IsEnum || Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true;
        }
    }
}

