using CodeSpirit.Aggregator.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace CodeSpirit.Aggregator.Examples
{
    /// <summary>
    /// 全局聚合器使用示例
    /// </summary>
    public static class GlobalAggregatorExample
    {
        /// <summary>
        /// 配置全局聚合器的示例方法
        /// </summary>
        /// <param name="services">服务集合</param>
        public static void ConfigureGlobalAggregator(IServiceCollection services)
        {
            // 示例1：使用预定义的常用规则
            services.AddCodeSpiritAggregator(globalConfig =>
            {
                globalConfig.ConfigureCommonGlobalRules();
            });

            // 示例2：自定义全局规则
            services.AddCodeSpiritAggregator(globalConfig =>
            {
                // 配置用户相关字段
                globalConfig.RegisterGlobalRule(
                    "CreatedBy", 
                    "http://identity/api/identity/internal/users/{value}.data.name", 
                    "{field}");
                
                globalConfig.RegisterGlobalRule(
                    "UpdatedBy", 
                    "http://identity/api/identity/internal/users/{value}.data.name", 
                    "{field}");

                // 配置部门字段
                globalConfig.RegisterGlobalRule(
                    "DepartmentId", 
                    "/api/departments/{value}.name", 
                    "部门: {field}");

                // 配置状态字段（静态模板）
                globalConfig.RegisterGlobalRule(
                    "Status", 
                    null, 
                    "状态: {value}");

                // 配置产品字段（动态补充）
                globalConfig.RegisterGlobalRule(
                    "ProductId", 
                    "/api/products/{value}.name", 
                    "{value} ({field})");
            });
        }
    }

    /// <summary>
    /// 示例DTO - 使用全局聚合规则
    /// </summary>
    public class DocumentDto
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        [DisplayName("文档ID")]
        public string Id { get; set; }

        /// <summary>
        /// 文档标题
        /// </summary>
        [DisplayName("标题")]
        public string Title { get; set; }

        /// <summary>
        /// 创建者ID - 将自动应用全局聚合规则
        /// 规则：http://identity/api/identity/internal/users/{value}.data.name#{field}
        /// </summary>
        [DisplayName("创建者")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// 更新者ID - 将自动应用全局聚合规则
        /// 规则：http://identity/api/identity/internal/users/{value}.data.name#{field}
        /// </summary>
        [DisplayName("更新者")]
        public string UpdatedBy { get; set; }

        /// <summary>
        /// 部门ID - 将自动应用全局聚合规则
        /// 规则：/api/departments/{value}.name#部门: {field}
        /// </summary>
        [DisplayName("部门")]
        public string DepartmentId { get; set; }

        /// <summary>
        /// 状态 - 将自动应用全局聚合规则（静态模板）
        /// 规则：#状态: {value}
        /// </summary>
        [DisplayName("状态")]
        public string Status { get; set; }

        /// <summary>
        /// 产品ID - 将自动应用全局聚合规则（动态补充）
        /// 规则：/api/products/{value}.name#{value} ({field})
        /// </summary>
        [DisplayName("产品")]
        public string ProductId { get; set; }

        /// <summary>
        /// 自定义字段 - 使用特性覆盖全局规则
        /// 特性规则优先级高于全局规则
        /// </summary>
        [DisplayName("自定义字段")]
        [CodeSpirit.Core.Attributes.AggregateField(
            dataSource: "/api/custom/{value}.displayName", 
            template: "自定义: {field}")]
        public string CustomField { get; set; }

        /// <summary>
        /// 普通字段 - 没有全局规则，不会被聚合
        /// </summary>
        [DisplayName("描述")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 示例DTO - 嵌套对象中的全局聚合规则
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [DisplayName("订单ID")]
        public string Id { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        [DisplayName("订单号")]
        public string OrderNumber { get; set; }

        /// <summary>
        /// 创建者 - 应用全局规则
        /// </summary>
        [DisplayName("创建者")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// 订单项列表 - 嵌套对象中的字段也会应用全局规则
        /// </summary>
        [DisplayName("订单项")]
        public List<OrderItemDto> Items { get; set; }
    }

    /// <summary>
    /// 订单项DTO
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [DisplayName("项目ID")]
        public string Id { get; set; }

        /// <summary>
        /// 产品ID - 嵌套对象中也会应用全局规则
        /// 最终字段路径：items.productId
        /// </summary>
        [DisplayName("产品")]
        public string ProductId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [DisplayName("数量")]
        public int Quantity { get; set; }

        /// <summary>
        /// 创建者 - 嵌套对象中也会应用全局规则
        /// 最终字段路径：items.createdBy
        /// </summary>
        [DisplayName("创建者")]
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// 运行时动态管理全局规则的示例服务
    /// </summary>
    public class DynamicRuleManagementService
    {
        private readonly IGlobalAggregatorConfigurationService _globalConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="globalConfig">全局聚合器配置服务</param>
        public DynamicRuleManagementService(IGlobalAggregatorConfigurationService globalConfig)
        {
            _globalConfig = globalConfig;
        }

        /// <summary>
        /// 添加新的全局规则
        /// </summary>
        public void AddCustomRule()
        {
            _globalConfig.RegisterGlobalRule(
                "CategoryId", 
                "/api/categories/{value}.name", 
                "分类: {field}");
        }

        /// <summary>
        /// 移除全局规则
        /// </summary>
        public void RemoveRule()
        {
            _globalConfig.RemoveGlobalRule("CategoryId");
        }

        /// <summary>
        /// 获取所有全局规则
        /// </summary>
        /// <returns>全局规则列表</returns>
        public IReadOnlyDictionary<string, GlobalAggregationRule> GetAllRules()
        {
            return _globalConfig.GetAllGlobalRules();
        }

        /// <summary>
        /// 检查特定字段是否有全局规则
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns>是否存在规则</returns>
        public bool HasRuleForField(string fieldName)
        {
            return _globalConfig.GetGlobalRule(fieldName) != null;
        }
    }
}
