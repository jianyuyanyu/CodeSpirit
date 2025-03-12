using Nest;
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
    /// 创建基于时间范围的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateTimeRangeQuery(
        DateTime startTime, 
        DateTime endTime)
    {
        return s => s
            .Query(q => q
                .DateRange(r => r
                    .Field(f => f.OperationTime)
                    .GreaterThanOrEquals(startTime)
                    .LessThanOrEquals(endTime)
                )
            );
    }
    
    /// <summary>
    /// 创建基于用户的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateUserQuery(string userId)
    {
        return s => s
            .Query(q => q
                .Term(t => t
                    .Field(f => f.UserId)
                    .Value(userId)
                )
            );
    }
    
    /// <summary>
    /// 创建基于控制器的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateControllerQuery(string controllerName)
    {
        return s => s
            .Query(q => q
                .Term(t => t
                    .Field(f => f.ControllerName)
                    .Value(controllerName)
                )
            );
    }
    
    /// <summary>
    /// 创建基于操作类型的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateOperationTypeQuery(string operationType)
    {
        return s => s
            .Query(q => q
                .Term(t => t
                    .Field(f => f.OperationType)
                    .Value(operationType)
                )
            );
    }
    
    /// <summary>
    /// 创建基于关键字的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateKeywordQuery(string keyword)
    {
        return s => s
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(f => f
                        .Field(ff => ff.Description)
                        .Field(ff => ff.RequestParams)
                        .Field(ff => ff.BeforeData)
                        .Field(ff => ff.AfterData)
                    )
                    .Query(keyword)
                    .Type(TextQueryType.BestFields)
                    .Operator(Operator.Or)
                )
            );
    }
    
    /// <summary>
    /// 创建基于IP地址的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateIpAddressQuery(string ipAddress)
    {
        return s => s
            .Query(q => q
                .Term(t => t
                    .Field(f => f.IpAddress)
                    .Value(ipAddress)
                )
            );
    }
    
    /// <summary>
    /// 创建基于成功状态的查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateSuccessQuery(bool isSuccess)
    {
        return s => s
            .Query(q => q
                .Term(t => t
                    .Field(f => f.IsSuccess)
                    .Value(isSuccess)
                )
            );
    }
    
    /// <summary>
    /// 创建统计聚合
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateStatsAggregation(
        string fieldName, 
        string aggregationName,
        int size = 20)
    {
        return s => s
            .Aggregations(a => a
                .Terms(aggregationName, t => t
                    .Field($"{fieldName}.keyword")
                    .Size(size)
                )
            );
    }
    
    /// <summary>
    /// 创建时间柱状图聚合
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateTimeHistogramAggregation(
        DateTime startTime, 
        DateTime endTime, 
        string interval = "1d")
    {
        return s => s
            .Aggregations(a => a
                .DateHistogram("time_histogram", h => h
                    .Field(f => f.OperationTime)
                    .CalendarInterval(interval)
                    .Format("yyyy-MM-dd'T'HH:mm:ss")
                    .MinimumDocumentCount(0)
                    .ExtendedBounds(
                        startTime,
                        endTime
                    )
                )
            );
    }
    
    /// <summary>
    /// 创建复杂聚合查询
    /// </summary>
    public static Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> CreateComplexAggregation(
        DateTime startTime, 
        DateTime endTime)
    {
        return s => s
            .Query(q => q
                .DateRange(r => r
                    .Field(f => f.OperationTime)
                    .GreaterThanOrEquals(startTime)
                    .LessThanOrEquals(endTime)
                )
            )
            .Aggregations(a => a
                .Terms("users", t => t
                    .Field(f => f.UserName.Suffix("keyword"))
                    .Size(10)
                    .Aggregations(aa => aa
                        .Terms("operations", tt => tt
                            .Field(f => f.OperationType.Suffix("keyword"))
                            .Size(10)
                        )
                    )
                )
                .DateHistogram("daily", h => h
                    .Field(f => f.OperationTime)
                    .CalendarInterval("1d")
                    .Format("yyyy-MM-dd")
                    .MinimumDocumentCount(0)
                    .ExtendedBounds(
                        startTime,
                        endTime
                    )
                    .Aggregations(aa => aa
                        .Terms("operations", tt => tt
                            .Field(f => f.OperationType.Suffix("keyword"))
                            .Size(10)
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
        
        // 解析用户统计
        if (aggregationResults.ContainsKey("users"))
        {
            var userStats = new Dictionary<string, object>();
            var users = aggregationResults["users"] as IEnumerable<object>;
            
            if (users != null)
            {
                foreach (dynamic userBucket in users)
                {
                    var operationStats = new Dictionary<string, long>();
                    if (((IDictionary<string, object>)userBucket).ContainsKey("operations"))
                    {
                        foreach (dynamic opBucket in userBucket.operations)
                        {
                            operationStats[opBucket.key.ToString()] = (long)opBucket.docCount;
                        }
                    }
                    
                    userStats[userBucket.key.ToString()] = new
                    {
                        Total = (long)userBucket.docCount,
                        Operations = operationStats
                    };
                }
            }
            
            result["UserStats"] = userStats;
        }
        
        // 解析日期统计
        if (aggregationResults.ContainsKey("daily"))
        {
            var dailyStats = new Dictionary<string, object>();
            var daily = aggregationResults["daily"] as IEnumerable<object>;
            
            if (daily != null)
            {
                foreach (dynamic dateBucket in daily)
                {
                    var operationStats = new Dictionary<string, long>();
                    if (((IDictionary<string, object>)dateBucket).ContainsKey("operations"))
                    {
                        foreach (dynamic opBucket in dateBucket.operations)
                        {
                            operationStats[opBucket.key.ToString()] = (long)opBucket.docCount;
                        }
                    }
                    
                    dailyStats[dateBucket.key.ToString()] = new
                    {
                        Total = (long)dateBucket.docCount,
                        Operations = operationStats
                    };
                }
            }
            
            result["DailyStats"] = dailyStats;
        }
        
        return result;
    }
} 