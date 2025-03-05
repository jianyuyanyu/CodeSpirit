using System;

namespace CodeSpirit.Aggregator.Services
{
    public interface IAggregationHeaderService
    {
        /// <summary>
        /// 从模型类型生成聚合规则头信息
        /// </summary>
        string GenerateAggregationHeader(Type modelType);
    }
} 