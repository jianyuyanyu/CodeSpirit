using System;
using System.Collections.Generic;

namespace CodeSpirit.Core.Extensions
{
    public static class CollectionExtensions
    {
        //
        // 摘要:
        //     Checks whatever given collection object is null or has no item.
        public static bool IsNullOrEmpty<T>(this ICollection<T> source)
        {
            if (source != null)
            {
                return source.Count <= 0;
            }

            return true;
        }

        //
        // 摘要:
        //     Adds an item to the collection if it's not already in the collection.
        //
        // 参数:
        //   source:
        //     Collection
        //
        //   item:
        //     Item to check and add
        //
        // 类型参数:
        //   T:
        //     Type of the items in the collection
        //
        // 返回结果:
        //     Returns True if added, returns False if not.
        public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.Contains(item))
            {
                return false;
            }

            source.Add(item);
            return true;
        }
    }
}
