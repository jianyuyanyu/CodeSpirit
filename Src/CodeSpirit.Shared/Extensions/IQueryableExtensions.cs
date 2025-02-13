using CodeSpirit.Core.Dtos;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;

namespace CodeSpirit.Shared.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string orderBy, string orderDir)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return query;
            }

            string sortField = orderBy.Trim().ToLower();
            string sortOrder = orderDir?.ToLower() == "desc" ? "descending" : "ascending";
            string ordering = $"{orderBy} {sortOrder}";

            try
            {
                return query.OrderBy(ordering);
            }
            catch (ParseException)
            {
                throw new ArgumentException("排序字段格式错误。");
            }
        }

        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, QueryDtoBase queryDto)
        {
            if (!string.IsNullOrWhiteSpace(queryDto.OrderBy))
            {
                query = query.ApplySorting<T>(queryDto.OrderBy, queryDto.OrderDir);
            }
            return query;
        }


        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int perPage)
        {
            int skip = (page - 1) * perPage;
            return query.Skip(skip).Take(perPage);
        }

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, QueryDtoBase queryDto)
        {
            int skip = (queryDto.Page - 1) * queryDto.PerPage;
            return query.Skip(skip).Take(queryDto.PerPage);
        }
    }

}
