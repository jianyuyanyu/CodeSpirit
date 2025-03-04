using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeSpirit.Aggregator.Services
{
    public interface IAggregatorService
    {
        /// <summary>
        /// 检查响应是否需要聚合处理
        /// </summary>
        bool NeedsAggregation(HttpResponseMessage response);
        
        /// <summary>
        /// 获取聚合规则
        /// </summary>
        Dictionary<string, string> GetAggregationRules(HttpResponseMessage response);
        
        /// <summary>
        /// 处理JSON内容进行聚合
        /// </summary>
        Task<string> AggregateJsonContent(string jsonContent, Dictionary<string, string> aggregationRules, HttpContext context);
    }
} 