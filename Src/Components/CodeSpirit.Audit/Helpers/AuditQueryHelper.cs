using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Helpers;

/// <summary>
/// 审计查询助手
/// </summary>
public static class AuditQueryHelper
{
    /// <summary>
    /// 创建用户查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateUserQuery(string userId)
    {
        return s => s.Query(q => q
            .Term(t => t
                .Field(f => f.UserId)
                .Value(userId)
            )
        );
    }
    
    /// <summary>
    /// 创建租户查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateTenantQuery(string tenantId)
    {
        return s => s.Query(q => q
            .Term(t => t
                .Field(f => f.TenantId)
                .Value(tenantId)
            )
        );
    }
    
    /// <summary>
    /// 创建操作类型查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateOperationQuery(string operation)
    {
        return s => s.Query(q => q
            .Term(t => t
                .Field(f => f.OperationType)
                .Value(operation)
            )
        );
    }
    
    /// <summary>
    /// 创建时间范围查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateTimeRangeQuery(
        DateTime? startTime = null, 
        DateTime? endTime = null)
    {
        return s => s.Query(q => q
            .Range(r => r
                .DateRange(dr => dr
                    .Field(f => f.OperationTime)
                    .Gte(startTime)
                    .Lte(endTime)
                )
            )
        );
    }
    
    /// <summary>
    /// 创建资源查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateResourceQuery(string resource)
    {
        return s => s.Query(q => q
            .Wildcard(w => w
                .Field(f => f.EntityName)
                .Value($"*{resource}*")
            )
        );
    }
    
    /// <summary>
    /// 创建IP地址查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateIPQuery(string ipAddress)
    {
        return s => s.Query(q => q
            .Term(t => t
                .Field(f => f.IpAddress)
                .Value(ipAddress)
            )
        );
    }
    
    /// <summary>
    /// 创建全文搜索查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateFullTextQuery(string searchText)
    {
        return s => s.Query(q => q
            .MultiMatch(m => m
                .Query(searchText)
                .Fields(new[] { "operationType", "entityName", "beforeData", "afterData", "userAgent" })
                .Type(TextQueryType.BestFields)
                .Operator(Operator.And)
            )
        );
    }
    
    /// <summary>
    /// 创建状态码查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateStatusCodeQuery(int statusCode)
    {
        return s => s.Query(q => q
            .Term(t => t
                .Field(f => f.StatusCode)
                .Value(statusCode)
            )
        );
    }
    
    /// <summary>
    /// 创建用户代理查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateUserAgentQuery(string userAgent)
    {
        return s => s.Query(q => q
            .Match(m => m
                .Field(f => f.UserAgent)
                .Query(userAgent)
            )
        );
    }
    
    /// <summary>
    /// 创建成功操作查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateSuccessfulOperationsQuery()
    {
        return s => s.Query(q => q
            .Bool(b => b
                .Must(m => m
                    .Range(r => r
                        .NumberRange(nr => nr
                            .Field(f => f.StatusCode)
                            .Gte(200)
                            .Lt(300)
                        )
                    )
                )
            )
        );
    }
    
    /// <summary>
    /// 创建失败操作查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateFailedOperationsQuery()
    {
        return s => s.Query(q => q
            .Bool(b => b
                .Must(m => m
                    .Range(r => r
                        .NumberRange(nr => nr
                            .Field(f => f.StatusCode)
                            .Gte(400)
                        )
                    )
                )
            )
        );
    }
    
    /// <summary>
    /// 解析聚合结果
    /// </summary>
    public static Dictionary<string, object> ParseAggregationResults(Dictionary<string, object> aggregationResults)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var item in aggregationResults)
        {
            if (item.Value is Dictionary<string, object> bucketData)
            {
                if (bucketData.ContainsKey("buckets"))
                {
                    result[item.Key] = bucketData["buckets"];
                }
                else if (bucketData.ContainsKey("value"))
                {
                    result[item.Key] = bucketData["value"];
                }
                else
                {
                    result[item.Key] = bucketData;
                }
            }
            else
            {
                result[item.Key] = item.Value;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 创建文本搜索查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateTextQuery(string searchText)
    {
        return s => s.Query(q => q
            .Bool(b => b
                .Should(should => should
                    .Match(m => m
                        .Field(f => f.UserName)
                        .Query(searchText)
                    )
                    .Match(m => m
                        .Field(f => f.OperationType)
                        .Query(searchText)
                    )
                    .Match(m => m
                        .Field(f => f.EntityName)
                        .Query(searchText)
                    )
                    .Match(m => m
                        .Field(f => f.Description)
                        .Query(searchText)
                    )
                )
            )
        );
    }
    
    /// <summary>
    /// 创建分页查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreatePaginationQuery(int pageIndex, int pageSize)
    {
        return s => s.From((pageIndex - 1) * pageSize).Size(pageSize);
    }
    
    /// <summary>
    /// 创建排序查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CreateSortQuery(string fieldName = "OperationTime", bool ascending = false)
    {
        return s =>
        {
            var elasticsearchFieldName = ConvertToElasticsearchFieldName(fieldName);
            
            return s.Sort(sort => sort
                .Field(elasticsearchFieldName, new FieldSort 
                { 
                    Order = ascending ? SortOrder.Asc : SortOrder.Desc 
                })
            );
        };
    }
    
    /// <summary>
    /// 将C#字段名称转换为Elasticsearch字段名称
    /// </summary>
    private static string ConvertToElasticsearchFieldName(string fieldName)
    {
        // 将PascalCase转换为camelCase
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;

        // 特殊字段映射
        var fieldMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "OperationTime", "operationTime" },
            { "UserId", "userId" },
            { "UserName", "userName" },
            { "IpAddress", "ipAddress" },
            { "ServiceName", "serviceName" },
            { "ControllerName", "controllerName" },
            { "ActionName", "actionName" },
            { "OperationType", "operationType" },
            { "RequestPath", "requestPath" },
            { "RequestMethod", "requestMethod" },
            { "EntityName", "entityName" },
            { "EntityId", "entityId" },
            { "ExecutionDuration", "executionDuration" },
            { "IsSuccess", "isSuccess" },
            { "StatusCode", "statusCode" }
        };

        if (fieldMappings.TryGetValue(fieldName, out var mappedName))
        {
            return mappedName;
        }

        // 默认转换：首字母小写
        return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
    }
    
    /// <summary>
    /// 组合多个查询
    /// </summary>
    public static Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>> CombineQueries(
        params Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>[] queries)
    {
        return s =>
        {
            var result = s;
            foreach (var query in queries)
            {
                result = query(result);
            }
            return result;
        };
    }
} 