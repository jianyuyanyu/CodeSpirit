using System;
using System.Collections.Generic;

namespace CodeSpirit.Core.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 提取 API 响应的数据类型。
        /// </summary>
        /// <param name="type">待分析的类型。</param>
        /// <returns>提取后的数据类型。</returns>
        public static Type ExtractDataType(this Type type)
        {
            if (type == null)
                return null;

            Type unwrappedType = type;

            // 递归提取数据类型
            while (unwrappedType != null && unwrappedType.IsGenericType)
            {
                Type genericTypeDef = unwrappedType.GetGenericTypeDefinition();

                // 处理 ApiResponse<T>
                if (genericTypeDef == typeof(ApiResponse<>))
                {
                    unwrappedType = unwrappedType.GetGenericArguments()[0];
                    continue; // 继续处理内部类型
                }

                // 处理各种集合类型
                if (genericTypeDef == typeof(PageList<>) ||
                    genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IReadOnlyList<>) ||
                    genericTypeDef == typeof(IReadOnlyCollection<>))
                {
                    return unwrappedType.GetGenericArguments()[0];
                }

                // 如果不是我们处理的特殊泛型类型，就直接返回
                break;
            }

            return unwrappedType;
        }
    }
}
