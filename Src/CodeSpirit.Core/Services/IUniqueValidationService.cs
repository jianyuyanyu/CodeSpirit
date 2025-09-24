using System;
using System.Threading.Tasks;

namespace CodeSpirit.Core;

/// <summary>
/// 唯一性验证服务接口
/// </summary>
public interface IUniqueValidationService
{
    /// <summary>
    /// 验证字段值的唯一性
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="value">字段值</param>
    /// <param name="excludeId">排除的实体ID（用于更新时排除自身）</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns>如果唯一则返回true，否则返回false</returns>
    Task<bool> IsUniqueAsync(
        Type entityType,
        string fieldName,
        string value,
        long? excludeId = null,
        bool ignoreCase = false);
}
