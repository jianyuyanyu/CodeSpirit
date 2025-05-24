using Elastic.Clients.Elasticsearch;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// Elasticsearch服务接口
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// 创建索引
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> CreateIndexAsync();
    
    /// <summary>
    /// 检查索引是否存在
    /// </summary>
    /// <returns>是否存在</returns>
    Task<bool> IndexExistsAsync();
    
    /// <summary>
    /// 索引文档
    /// </summary>
    /// <typeparam name="T">文档类型</typeparam>
    /// <param name="document">文档</param>
    /// <returns>是否成功</returns>
    Task<bool> IndexDocumentAsync<T>(T document) where T : class;
    
    /// <summary>
    /// 批量索引文档
    /// </summary>
    /// <typeparam name="T">文档类型</typeparam>
    /// <param name="documents">文档集合</param>
    /// <returns>是否成功</returns>
    Task<bool> BulkIndexAsync<T>(IEnumerable<T> documents) where T : class;
    
    /// <summary>
    /// 获取文档
    /// </summary>
    /// <typeparam name="T">文档类型</typeparam>
    /// <param name="id">文档ID</param>
    /// <returns>文档</returns>
    Task<T?> GetDocumentAsync<T>(string id) where T : class;
    
    /// <summary>
    /// 搜索文档
    /// </summary>
    /// <typeparam name="T">文档类型</typeparam>
    /// <param name="searchFunc">搜索函数</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<T> Items, long Total)> SearchAsync<T>(Func<SearchRequestDescriptor<T>, SearchRequestDescriptor<T>> searchFunc) where T : class;
    
    /// <summary>
    /// 删除文档
    /// </summary>
    /// <param name="id">文档ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteDocumentAsync(string id);
    
    /// <summary>
    /// 聚合查询
    /// </summary>
    /// <typeparam name="T">文档类型</typeparam>
    /// <param name="aggregationFunc">聚合函数</param>
    /// <returns>聚合结果</returns>
    Task<IDictionary<string, object>?> AggregateAsync<T>(Func<SearchRequestDescriptor<T>, SearchRequestDescriptor<T>> aggregationFunc) where T : class;
} 