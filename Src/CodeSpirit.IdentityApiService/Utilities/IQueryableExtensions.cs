using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;

namespace CodeSpirit.IdentityApi.Utilities
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string orderBy, string orderDir, List<string> allowedSortFields)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return query;
            }

            var sortField = orderBy.Trim().ToLower();
            if (!allowedSortFields.Contains(sortField))
            {
                throw new ArgumentException($"不支持的排序字段: {orderBy}");
            }

            var sortOrder = orderDir?.ToLower() == "desc" ? "descending" : "ascending";
            var ordering = $"{orderBy} {sortOrder}";

            try
            {
                return query.OrderBy(ordering);
            }
            catch (ParseException)
            {
                throw new ArgumentException("排序字段格式错误。");
            }
        }

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int perPage)
        {
            var skip = (page - 1) * perPage;
            return query.Skip(skip).Take(perPage);
        }
    }

}
