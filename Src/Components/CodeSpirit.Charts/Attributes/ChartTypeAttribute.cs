using CodeSpirit.Charts.Models;

namespace CodeSpirit.Charts.Attributes
{
    /// <summary>
    /// 图表类型特性，用于指定图表的类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class ChartTypeAttribute : Attribute
    {
        /// <summary>
        /// 图表类型
        /// </summary>
        public ChartType Type { get; set; }
        
        /// <summary>
        /// 图表子类型
        /// </summary>
        public string? SubType { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">图表类型</param>
        public ChartTypeAttribute(ChartType type)
        {
            Type = type;
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">图表类型</param>
        /// <param name="subType">图表子类型</param>
        public ChartTypeAttribute(ChartType type, string subType)
        {
            Type = type;
            SubType = subType;
        }
    }
} 