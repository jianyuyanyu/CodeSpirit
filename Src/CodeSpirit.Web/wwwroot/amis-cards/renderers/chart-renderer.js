/**
 * 图表卡片渲染器
 * 支持线图、柱状图、饼图等多种图表类型
 * 
 * @author CodeSpirit
 * @version 2.0
 */

(function() {
    'use strict';
    
    // 确保命名空间存在
    window.AmisCards = window.AmisCards || {};
    
    /**
     * 图表卡片渲染器类
     * 继承自BaseRenderer，专门用于渲染各种图表卡片
     */
    class ChartRenderer extends window.AmisCards.BaseRenderer {
        /**
         * 构造函数
         * @param {Object} config - 图表配置
         */
        constructor(config) {
            super(config);
            
            // 图表类型映射
            this.chartTypeMap = {
                'line': 'line',
                'bar': 'column',
                'pie': 'pie',
                'area': 'area',
                'scatter': 'scatter',
                'radar': 'radar'
            };
            
            // 默认图表配置
            this.defaultChartConfig = {
                tooltip: {
                    show: true
                },
                legend: {
                    show: true,
                    position: 'bottom'
                },
                grid: {
                    left: '3%',
                    right: '4%',
                    bottom: '10%',
                    containLabel: true
                }
            };
        }
        
        /**
         * 获取渲染器类型
         * @returns {string} 渲染器类型
         */
        getType() {
            return 'chart';
        }
        
        /**
         * 生成Amis配置
         * @returns {Object} Amis页面配置
         */
        generateAmisConfig() {
            // 不调用super.generateAmisConfig()，因为它是抽象方法
            const baseConfig = this.getBaseAmisConfig();
            
            return {
                ...baseConfig,
                body: this.getCardBody(),
                className: `${baseConfig.className} amis-cards-chart`
            };
        }
        
        /**
         * 获取卡片主体内容
         * @returns {Array} Amis 组件配置
         */
        getCardBody() {
            const chartConfig = this.buildChartConfig();
            
            return [
                {
                    type: 'chart',
                    className: 'amis-cards-chart',
                    api: this.config.data?.api || this.config.api,
                    config: chartConfig,
                    height: this.config.height || 300,
                    ...this.buildChartProps()
                }
            ];
        }
        
        /**
         * 构建图表配置
         * @returns {Object} 图表配置对象
         */
        buildChartConfig() {
            const chartType = this.config.chartType || 'line';
            const baseConfig = {
                ...this.defaultChartConfig,
                title: {
                    text: this.config.showTitle !== false ? this.config.title : '',
                    textStyle: {
                        fontSize: 14,
                        fontWeight: 'normal'
                    }
                }
            };
            
            // 根据图表类型添加特定配置
            switch (chartType) {
                case 'line':
                    return this.buildLineChartConfig(baseConfig);
                case 'bar':
                    return this.buildBarChartConfig(baseConfig);
                case 'pie':
                    return this.buildPieChartConfig(baseConfig);
                case 'area':
                    return this.buildAreaChartConfig(baseConfig);
                case 'scatter':
                    return this.buildScatterChartConfig(baseConfig);
                case 'radar':
                    return this.buildRadarChartConfig(baseConfig);
                default:
                    return baseConfig;
            }
        }
        
        /**
         * 构建线图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 线图配置
         */
        buildLineChartConfig(baseConfig) {
            return {
                ...baseConfig,
                xAxis: {
                    type: 'category',
                    boundaryGap: false,
                    data: this.config.xAxisData || [],
                    ...this.config.xAxis
                },
                yAxis: {
                    type: 'value',
                    ...this.config.yAxis
                },
                series: this.config.series?.map(s => ({
                    ...s,
                    type: 'line',
                    smooth: this.config.smooth !== false,
                    symbol: this.config.showSymbol !== false ? 'circle' : 'none',
                    symbolSize: this.config.symbolSize || 4
                })) || []
            };
        }
        
        /**
         * 构建柱状图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 柱状图配置
         */
        buildBarChartConfig(baseConfig) {
            return {
                ...baseConfig,
                xAxis: {
                    type: 'category',
                    data: this.config.xAxisData || [],
                    ...this.config.xAxis
                },
                yAxis: {
                    type: 'value',
                    ...this.config.yAxis
                },
                series: this.config.series?.map(s => ({
                    ...s,
                    type: 'bar',
                    barWidth: this.config.barWidth || '60%'
                })) || []
            };
        }
        
        /**
         * 构建饼图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 饼图配置
         */
        buildPieChartConfig(baseConfig) {
            return {
                ...baseConfig,
                series: [{
                    type: 'pie',
                    radius: this.config.radius || ['0%', '70%'],
                    center: this.config.center || ['50%', '50%'],
                    data: this.config.data?.data || this.config.pieData || [],
                    emphasis: {
                        itemStyle: {
                            shadowBlur: 10,
                            shadowOffsetX: 0,
                            shadowColor: 'rgba(0, 0, 0, 0.5)'
                        }
                    },
                    ...this.config.series?.[0]
                }]
            };
        }
        
        /**
         * 构建面积图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 面积图配置
         */
        buildAreaChartConfig(baseConfig) {
            const lineConfig = this.buildLineChartConfig(baseConfig);
            return {
                ...lineConfig,
                series: lineConfig.series.map(s => ({
                    ...s,
                    areaStyle: this.config.areaStyle || {}
                }))
            };
        }
        
        /**
         * 构建散点图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 散点图配置
         */
        buildScatterChartConfig(baseConfig) {
            return {
                ...baseConfig,
                xAxis: {
                    type: 'value',
                    ...this.config.xAxis
                },
                yAxis: {
                    type: 'value',
                    ...this.config.yAxis
                },
                series: this.config.series?.map(s => ({
                    ...s,
                    type: 'scatter',
                    symbolSize: this.config.symbolSize || 20
                })) || []
            };
        }
        
        /**
         * 构建雷达图配置
         * @param {Object} baseConfig - 基础配置
         * @returns {Object} 雷达图配置
         */
        buildRadarChartConfig(baseConfig) {
            return {
                ...baseConfig,
                radar: {
                    indicator: this.config.indicator || [],
                    ...this.config.radar
                },
                series: [{
                    type: 'radar',
                    data: this.config.series || [],
                    ...this.config.radarSeries
                }]
            };
        }
        
        /**
         * 构建图表属性
         * @returns {Object} 图表属性
         */
        buildChartProps() {
            const props = {};
            
            // 数据映射
            if (this.config.dataMapping) {
                props.dataMapping = this.config.dataMapping;
            }
            
            // 交互配置
            if (this.config.clickAction) {
                props.clickAction = this.config.clickAction;
            }
            
            // 刷新配置
            if (this.config.interval) {
                props.interval = this.config.interval;
            }
            
            return props;
        }
        
        /**
         * 获取设置表单配置
         * @returns {Object} 设置表单配置
         */
        getSettingsForm() {
            return {
                type: 'form',
                title: '图表设置',
                body: [
                    ...super.getSettingsForm().body,
                    {
                        type: 'divider',
                        title: '图表配置'
                    },
                    {
                        type: 'select',
                        name: 'chartType',
                        label: '图表类型',
                        value: this.config.chartType || 'line',
                        options: [
                            { label: '线图', value: 'line' },
                            { label: '柱状图', value: 'bar' },
                            { label: '饼图', value: 'pie' },
                            { label: '面积图', value: 'area' },
                            { label: '散点图', value: 'scatter' },
                            { label: '雷达图', value: 'radar' }
                        ]
                    },
                    {
                        type: 'number',
                        name: 'height',
                        label: '图表高度',
                        value: this.config.height || 300,
                        min: 200,
                        max: 800
                    },
                    {
                        type: 'switch',
                        name: 'showTitle',
                        label: '显示标题',
                        value: this.config.showTitle !== false
                    },
                    {
                        type: 'switch',
                        name: 'smooth',
                        label: '平滑曲线',
                        value: this.config.smooth !== false,
                        visibleOn: '${chartType === "line" || chartType === "area"}'
                    },
                    {
                        type: 'switch',
                        name: 'showSymbol',
                        label: '显示数据点',
                        value: this.config.showSymbol !== false,
                        visibleOn: '${chartType === "line" || chartType === "area"}'
                    },
                    {
                        type: 'textarea',
                        name: 'customConfig',
                        label: '自定义配置',
                        description: '输入JSON格式的ECharts配置',
                        value: JSON.stringify(this.config.customConfig || {}, null, 2)
                    }
                ]
            };
        }
        
        /**
         * 获取调试信息
         * @returns {Object} 调试信息
         */
        getDebugInfo() {
            return {
                ...super.getDebugInfo(),
                chartType: this.config.chartType || 'line',
                height: this.config.height || 300,
                seriesCount: this.config.series?.length || 0,
                hasCustomConfig: !!this.config.customConfig
            };
        }
        
        /**
         * 验证配置
         * @returns {Object} 验证结果
         */
        validateConfig() {
            const result = super.validateConfig();
            
            // 验证图表类型
            const chartType = this.config.chartType;
            if (chartType && !this.chartTypeMap[chartType]) {
                result.errors.push(`不支持的图表类型: ${chartType}`);
            }
            
            // 验证数据源
            if (!this.config.data?.api && !this.config.api && !this.config.series && !this.config.pieData) {
                result.errors.push('图表需要配置数据源或静态数据');
            }
            
            // 验证饼图数据
            if (chartType === 'pie' && !this.config.pieData && !this.config.data?.data) {
                result.errors.push('饼图需要配置pieData或data.data');
            }
            
            // 验证雷达图配置
            if (chartType === 'radar' && !this.config.indicator) {
                result.errors.push('雷达图需要配置indicator');
            }
            
            return result;
        }
    }
    
    // 注册到全局命名空间
    window.AmisCards.ChartRenderer = ChartRenderer;
    
    // 注册到全局渲染器
    window.AmisCards.registerRenderer = window.AmisCards.registerRenderer || function(type, renderer) {
        console.log(`[AmisCards] 全局注册渲染器: ${type}`);
        window.AmisCards.renderers = window.AmisCards.renderers || new Map();
        window.AmisCards.renderers.set(type, renderer);
    };
    
    window.AmisCards.registerRenderer('chart', ChartRenderer);
    
    console.log('[AmisCards] ChartRenderer 已加载');
    
})(); 