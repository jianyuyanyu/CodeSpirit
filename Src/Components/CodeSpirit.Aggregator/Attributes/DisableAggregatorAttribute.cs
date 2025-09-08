using System;

namespace CodeSpirit.Aggregator.Attributes
{
    /// <summary>
    /// 禁用聚合器特性
    /// 用于标记控制器或方法不需要聚合器处理
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DisableAggregatorAttribute : Attribute
    {
        /// <summary>
        /// 初始化 DisableAggregatorAttribute 特性
        /// </summary>
        public DisableAggregatorAttribute()
        {
        }
    }
}
