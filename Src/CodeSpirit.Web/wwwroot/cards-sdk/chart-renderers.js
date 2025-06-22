/**
 * CodeSpirit Cards SDK - 图表渲染器集合
 * 包含基础图表渲染器和Amis图表渲染器
 * @version 1.0.2
 * @author CodeSpirit Team
 */

// 将图表渲染器注册到全局作用域
(function() {

/**
 * 基础图表卡片渲染器
 * 基于 ECharts 的图表渲染实现
 */
class ChartCardRenderer {
    async render(config) {
        const card = document.createElement('div');
        card.className = `card chart-card theme-${config.style?.theme || 'default'}`;
        
        const chartId = `chart-${config.id}`;
        
        card.innerHTML = `
            <div class="card-header">
                ${config.title ? `<h4 class="card-title">${config.title}</h4>` : ''}
                ${config.subtitle ? `<p class="card-subtitle">${config.subtitle}</p>` : ''}
            </div>
            <div class="card-body">
                <div id="${chartId}" class="chart-container" style="height: ${config.style?.height || 300}px;"></div>
            </div>
        `;
        
        // 初始化图表
        setTimeout(() => {
            this.initChart(chartId, config);
        }, 100);
        
        return card;
    }

    async update(element, config) {
        const chartContainer = element.querySelector('.chart-container');
        if (chartContainer && window.echarts) {
            const chart = window.echarts.getInstanceByDom(chartContainer);
            if (chart) {
                chart.setOption(this.getChartOption(config));
            }
        }
    }

    initChart(chartId, config) {
        if (!window.echarts) {
            console.error('ECharts 未加载');
            return;
        }
        
        const container = document.getElementById(chartId);
        if (!container) return;
        
        const chart = window.echarts.init(container);
        chart.setOption(this.getChartOption(config));
    }

    getChartOption(config) {
        // 基础配置
        return {
            title: {
                text: config.data.chartTitle || '',
                left: 'center'
            },
            tooltip: {
                trigger: 'axis'
            },
            xAxis: {
                type: 'category',
                data: config.data.xData || []
            },
            yAxis: {
                type: 'value'
            },
            series: [{
                type: config.data.chartType || 'line',
                data: config.data.yData || []
            }]
        };
    }
}

/**
 * Amis图表卡片渲染器
 * 使用Amis Chart组件渲染图表，提供与Amis主题一致的样式
 * 参考文档: https://aisuda.bce.baidu.com/amis/zh-CN/components/chart
 */
class AmisChartCardRenderer {
    async render(config) {
        const card = document.createElement('div');
        card.className = `card amis-chart-card theme-${config.style?.theme || 'default'}`;
        
        const chartId = `amis-chart-${config.id}`;
        
        card.innerHTML = `
            <div class="card-header">
                ${config.title ? `<h4 class="card-title">${config.title}</h4>` : ''}
                ${config.subtitle ? `<p class="card-subtitle">${config.subtitle}</p>` : ''}
                <div class="card-badge">Amis Chart</div>
            </div>
            <div class="card-body">
                <div id="${chartId}" class="amis-chart-container" style="height: ${config.style?.height || 300}px;"></div>
            </div>
        `;
        
        // 初始化Amis图表
        setTimeout(() => {
            this.initAmisChart(chartId, config);
        }, 100);
        
        return card;
    }

    async update(element, config) {
        const chartContainer = element.querySelector('.amis-chart-container');
        if (chartContainer) {
            // 重新渲染Amis图表
            this.initAmisChart(chartContainer.id, config);
        }
    }

    /**
     * 初始化Amis图表
     */
    initAmisChart(chartId, config) {
        const container = document.getElementById(chartId);
        if (!container) {
            console.error(`图表容器 ${chartId} 未找到`);
            return;
        }

        console.log('初始化Amis图表:', chartId, config);

        try {
            console.log('使用Amis图表渲染');
            const amisConfig = this.getAmisChartConfig(config);
            this.renderAmisChart(container, amisConfig);
        } catch (error) {
            console.error('渲染Amis图表失败:', error);
            this.renderError(container, '图表渲染失败');
        }
    }

    /**
     * 生成Amis Chart配置
     * 参考Amis Chart组件文档: https://aisuda.bce.baidu.com/amis/zh-CN/components/chart
     */
    getAmisChartConfig(config) {
        const chartData = config.data || {};
        const chartType = chartData.chartType || 'line';
        const themeColor = this.getThemeColor(config.style?.theme);
        
        // 标准Amis Chart组件配置
        const amisConfig = {
            type: 'chart',
            height: config.style?.height || 300,
            config: this.getEChartsConfig(chartData, chartType, themeColor)
        };

        // 处理数据源配置 - 根据Amis官方文档规范
        if (chartData.api) {
            // 如果有API数据源，使用api字段
            amisConfig.api = chartData.api;
        } else if (chartData.data) {
            // 如果有静态数据，直接设置数据
            amisConfig.data = chartData.data;
        } else if (chartData.source) {
            // 如果有数据源表达式，使用source字段
            amisConfig.source = chartData.source;
        } else {
            // 默认使用图表数据构建静态数据集
            amisConfig.data = {
                chartData: {
                    xData: chartData.xData || [],
                    yData: chartData.yData || [],
                    series: chartData.series || [],
                    pieData: chartData.pieData || []
                }
            };
        }

        return amisConfig;
    }

    /**
     * 生成标准ECharts配置
     * 确保与ECharts官方配置格式一致
     */
    getEChartsConfig(chartData, chartType, themeColor) {
        const config = {
            // 标题配置
            title: {
                text: chartData.chartTitle || '',
                left: 'center',
                textStyle: {
                    fontSize: 16,
                    fontWeight: 'normal',
                    color: '#333'
                }
            },
            
            // 工具提示
            tooltip: {
                trigger: chartType === 'pie' ? 'item' : 'axis',
                backgroundColor: 'rgba(0, 0, 0, 0.8)',
                borderColor: 'transparent',
                textStyle: {
                    color: '#fff'
                },
                formatter: chartType === 'pie' ? '{a} <br/>{b}: {c} ({d}%)' : undefined
            },
            
            // 图例
            legend: {
                show: chartType === 'pie' || (chartData.series && chartData.series.length > 1),
                orient: 'horizontal',
                x: 'center',
                y: 'bottom',
                data: this.getLegendData(chartData, chartType)
            },
            
            // 网格配置
            grid: chartType !== 'pie' ? {
                left: '3%',
                right: '4%',
                bottom: '3%',
                top: chartData.chartTitle ? '15%' : '3%',
                containLabel: true
            } : undefined
        };

        // 根据图表类型添加坐标轴配置
        if (chartType !== 'pie') {
            config.xAxis = this.getXAxisConfig(chartData);
            config.yAxis = this.getYAxisConfig(chartData);
        }

        // 配置数据系列
        config.series = this.getSeriesConfig(chartData, chartType, themeColor);

        return config;
    }

    /**
     * 获取X轴配置
     */
    getXAxisConfig(chartData) {
        return {
            type: 'category',
            data: chartData.xData || [],
            axisTick: {
                alignWithLabel: true
            },
            axisLine: {
                lineStyle: {
                    color: '#e1e4e8'
                }
            },
            axisLabel: {
                color: '#666'
            }
        };
    }

    /**
     * 获取Y轴配置
     */
    getYAxisConfig(chartData) {
        return {
            type: 'value',
            axisLine: {
                show: false
            },
            axisTick: {
                show: false
            },
            axisLabel: {
                color: '#666'
            },
            splitLine: {
                lineStyle: {
                    color: '#f0f0f0',
                    type: 'dashed'
                }
            }
        };
    }

    /**
     * 获取图例数据
     */
    getLegendData(chartData, chartType) {
        if (chartType === 'pie') {
            return chartData.pieData ? chartData.pieData.map(item => item.name) : [];
        }
        
        if (chartData.series && chartData.series.length > 1) {
            return chartData.series.map(series => series.name);
        }
        
        return [];
    }

    /**
     * 获取数据系列配置
     */
    getSeriesConfig(chartData, chartType, themeColor) {
        switch (chartType) {
            case 'pie':
                return this.getPieSeriesConfig(chartData, themeColor);
            case 'bar':
                return this.getBarSeriesConfig(chartData, themeColor);
            case 'line':
                return this.getLineSeriesConfig(chartData, themeColor);
            case 'area':
                return this.getAreaSeriesConfig(chartData, themeColor);
            default:
                return this.getLineSeriesConfig(chartData, themeColor);
        }
    }

    /**
     * 饼图系列配置
     */
    getPieSeriesConfig(chartData, themeColor) {
        return [{
            type: 'pie',
            radius: ['40%', '70%'],
            center: ['50%', '45%'],
            data: chartData.pieData || [],
            emphasis: {
                itemStyle: {
                    shadowBlur: 10,
                    shadowOffsetX: 0,
                    shadowColor: 'rgba(0, 0, 0, 0.5)'
                }
            },
            itemStyle: {
                borderRadius: 5,
                borderColor: '#fff',
                borderWidth: 2
            }
        }];
    }

    /**
     * 柱状图系列配置
     */
    getBarSeriesConfig(chartData, themeColor) {
        if (chartData.series && chartData.series.length > 0) {
            return chartData.series.map((series, index) => ({
                name: series.name,
                type: 'bar',
                data: series.data,
                itemStyle: {
                    borderRadius: [4, 4, 0, 0],
                    color: this.getSeriesColor(themeColor, index)
                }
            }));
        }
        
        return [{
            name: chartData.chartTitle || '数据',
            type: 'bar',
            data: chartData.yData || [],
            itemStyle: {
                borderRadius: [4, 4, 0, 0],
                color: themeColor
            }
        }];
    }

    /**
     * 折线图系列配置
     */
    getLineSeriesConfig(chartData, themeColor) {
        if (chartData.series && chartData.series.length > 0) {
            return chartData.series.map((series, index) => ({
                name: series.name,
                type: 'line',
                data: series.data,
                smooth: true,
                lineStyle: {
                    width: 3,
                    color: this.getSeriesColor(themeColor, index)
                },
                itemStyle: {
                    color: this.getSeriesColor(themeColor, index)
                }
            }));
        }
        
        return [{
            name: chartData.chartTitle || '数据',
            type: 'line',
            data: chartData.yData || [],
            smooth: true,
            lineStyle: {
                width: 3,
                color: themeColor
            },
            itemStyle: {
                color: themeColor
            }
        }];
    }

    /**
     * 面积图系列配置
     */
    getAreaSeriesConfig(chartData, themeColor) {
        const lineConfig = this.getLineSeriesConfig(chartData, themeColor);
        return lineConfig.map(series => ({
            ...series,
            areaStyle: {
                opacity: 0.3,
                color: {
                    type: 'linear',
                    x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [
                        { offset: 0, color: series.itemStyle.color + '4D' },
                        { offset: 1, color: series.itemStyle.color + '1A' }
                    ]
                }
            }
        }));
    }

    /**
     * 获取系列颜色
     */
    getSeriesColor(baseColor, index) {
        const colors = [
            baseColor,
            '#52c41a',
            '#faad14',
            '#ff4d4f',
            '#13c2c2',
            '#722ed1',
            '#eb2f96'
        ];
        return colors[index % colors.length];
    }

    /**
     * 获取主题颜色
     */
    getThemeColor(theme) {
        const colors = {
            'default': '#1890ff',
            'primary': '#1890ff',
            'success': '#52c41a',
            'warning': '#faad14',
            'danger': '#ff4d4f',
            'info': '#13c2c2'
        };
        return colors[theme] || colors.default;
    }

    /**
     * 渲染Amis图表
     * 使用标准的Amis Chart组件渲染方式
     */
    renderAmisChart(container, config) {
        console.log('开始渲染Amis图表:', { container, config });
        
        try {
            // 使用amis.embed方法（需要ID选择器）
            if (window.amis && typeof window.amis.embed === 'function') {
                console.log('使用 amis.embed 方法');
                container.innerHTML = '';
                
                // 确保容器有ID，amis.embed只支持ID选择器
                let containerId = container.id;
                if (!containerId) {
                    containerId = 'amis-chart-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                    container.id = containerId;
                }
                
                // 构建数据上下文 - 第三个参数
                const amisOptions = {
                    // 位置信息
                    location: window.location,
                    
                    // 全局数据绑定 - 根据Amis官方文档规范
                    data: {},
                    
                    // 其他上下文信息
                    context: {
                        cardId: config.id,
                        cardTitle: config.title || ''
                    }
                };
                
                // 处理数据绑定
                if (config.data && !config.api) {
                    // 静态数据：合并到全局数据上下文
                    Object.assign(amisOptions.data, config.data);
                }
                
                // 添加通用全局变量
                Object.assign(amisOptions.data, {
                    currentTime: new Date().toISOString(),
                    cardId: config.id,
                    cardTitle: config.title || ''
                });
                
                // 构建处理器配置 - 第四个参数
                const amisHandlers = {
                    // 主题配置
                    theme: 'antd',
                    
                    // 请求适配器
                    requestAdaptor: (api) => {
                        console.log(`🚀 API请求 [${config.id}]:`, api);
                        
                        // 添加认证信息
                        if (!window.TokenManager) {
                            throw new Error('TokenManager is required but not found.');
                        }
                        
                        const authHeaders = window.TokenManager.getAuthHeaders();
                        
                        return {
                            ...api,
                            headers: {
                                'Content-Type': 'application/json',
                                'X-Card-ID': config.id,
                                'X-Card-Type': 'chart',
                                'X-SDK-Version': '1.0.0',
                                ...api.headers,
                                ...authHeaders
                            }
                        };
                    },
                    
                    // 响应适配器
                    responseAdaptor: (api, payload, query, request, response) => {
                        console.log(`📥 API响应 [${config.id}]:`, {
                            api: api.url,
                            status: response?.status,
                            payload
                        });
                        
                        // 处理认证失败
                        if (payload?.status === 401 || response?.status === 401) {
                            console.warn('🔐 认证失败，需要重新登录');
                            if (window.TokenManager) {
                                window.TokenManager.clearToken();
                            }
                            return {
                                status: 1,
                                msg: '认证失败，请重新登录',
                                data: {}
                            };
                        }
                        
                        return payload;
                    }
                };
                
                console.log('🎯 Amis嵌入配置:', {
                    selector: '#' + containerId,
                    schema: config,
                    options: amisOptions,
                    handlers: amisHandlers
                });
                
                // 使用embed方法渲染 - 标准的四参数格式
                const amisInstance = window.amis.embed('#' + containerId, config, amisOptions, amisHandlers);
                
                return amisInstance;
            }
            
            // 方式3: 通过amisRequire加载
            if (window.amisRequire) {
                console.log('使用 amisRequire 方法');
                const amis = window.amisRequire('amis/embed');
                
                if (amis && amis.embed) {
                    // 确保容器有ID，amis.embed只支持ID选择器
                    let containerId = container.id;
                    if (!containerId) {
                        containerId = 'amis-chart-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                        container.id = containerId;
                    }
                    
                    // 构建数据上下文 - 第三个参数
                    const amisOptions = {
                        // 位置信息
                        location: window.location,
                        
                        // 全局数据绑定 - 根据Amis官方文档规范
                        data: {},
                        
                        // 其他上下文信息
                        context: {
                            cardId: config.id,
                            cardTitle: config.title || ''
                        }
                    };
                    
                    // 处理数据绑定
                    if (config.data && !config.api) {
                        // 静态数据：合并到全局数据上下文
                        Object.assign(amisOptions.data, config.data);
                    }
                    
                    // 添加通用全局变量
                    Object.assign(amisOptions.data, {
                        currentTime: new Date().toISOString(),
                        cardId: config.id,
                        cardTitle: config.title || ''
                    });
                    
                    // 构建处理器配置 - 第四个参数
                    const amisHandlers = {
                        // 主题配置
                        theme: 'antd',
                        
                        // 请求适配器
                        requestAdaptor: (api) => {
                            console.log(`🚀 API请求 [${config.id}]:`, api);
                            
                            // 添加认证信息
                            if (!window.TokenManager) {
                                throw new Error('TokenManager is required but not found.');
                            }
                            
                            const authHeaders = window.TokenManager.getAuthHeaders();
                            
                            return {
                                ...api,
                                headers: {
                                    'Content-Type': 'application/json',
                                    'X-Card-ID': config.id,
                                    'X-Card-Type': 'chart',
                                    'X-SDK-Version': '1.0.0',
                                    ...api.headers,
                                    ...authHeaders
                                }
                            };
                        },
                        
                        // 响应适配器
                        responseAdaptor: (api, payload, query, request, response) => {
                            console.log(`📥 API响应 [${config.id}]:`, {
                                api: api.url,
                                status: response?.status,
                                payload
                            });
                            
                            // 处理认证失败
                            if (payload?.status === 401 || response?.status === 401) {
                                console.warn('🔐 认证失败，需要重新登录');
                                if (window.TokenManager) {
                                    window.TokenManager.clearToken();
                                }
                                return {
                                    status: 1,
                                    msg: '认证失败，请重新登录',
                                    data: {}
                                };
                            }
                            
                            return payload;
                        }
                    };
                    
                    console.log('🎯 Amis嵌入配置 (amisRequire):', {
                        selector: '#' + containerId,
                        schema: config,
                        options: amisOptions,
                        handlers: amisHandlers
                    });
                    
                    // 使用embed方法渲染 - 标准的四参数格式
                    const amisInstance = amis.embed('#' + containerId, config, amisOptions, amisHandlers);
                    
                    return amisInstance;
                }
            }
            
            throw new Error('未找到可用的Amis渲染方法，请确保已正确加载Amis');
            
        } catch (error) {
            console.error('Amis图表渲染失败:', error);
            console.error('配置信息:', JSON.stringify(config, null, 2));
            throw error;
        }
    }

    /**
     * 渲染错误信息
     */
    renderError(container, message) {
        container.innerHTML = `
            <div class="amis-chart-error">
                <div class="error-content">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>${message}</p>
                </div>
            </div>
        `;
    }
}

// 全局注册图表渲染器
if (typeof window !== 'undefined') {
    window.ChartCardRenderer = ChartCardRenderer;
    window.AmisChartCardRenderer = AmisChartCardRenderer;
    console.log('✅ 图表渲染器已注册到全局作用域');
}

})(); 