/**
 * CodeSpirit 统计卡片前端SDK
 * @version 1.0.0
 * @author CodeSpirit Team
 */
(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = global || self, factory(global.CodeSpiritCards = {}));
}(this, (function (exports) { 'use strict';

    /**
     * 统计卡片SDK主类
     */
    class CodeSpiritCardsSDK {
        constructor(options = {}) {
            this.options = {
                container: '#cards-container',
                baseUrl: '/api',
                theme: 'default',
                autoRefresh: true,
                refreshInterval: 30000,
                ...options
            };
            
            this.cards = new Map();
            this.renderers = new Map();
            this.eventBus = new EventBus();
            this.dataService = new DataService(this.options.baseUrl);
            
            this.init();
        }

        /**
         * 初始化SDK
         */
        init() {
            // 注册默认渲染器
            this.registerRenderer('stat', new StatCardRenderer());
            this.registerRenderer('chart', new ChartCardRenderer());
            this.registerRenderer('amis-chart', new AmisChartCardRenderer());
            this.registerRenderer('info', new InfoCardRenderer());
            this.registerRenderer('action', new ActionCardRenderer());
            
            // 初始化样式
            this.loadStyles();
            window.amis = amisRequire('amis/embed');
            console.log('CodeSpirit Cards SDK 初始化完成');
        }

        /**
         * 渲染卡片组
         * @param {string} containerId 容器ID
         * @param {Array} configs 卡片配置数组
         */
        async render(containerId, configs) {
            const container = document.querySelector(containerId);
            if (!container) {
                throw new Error(`容器 ${containerId} 不存在`);
            }

            // 清空容器
            container.innerHTML = '';
            
            // 创建网格容器
            const gridContainer = this.createGridContainer(configs.length);
            container.appendChild(gridContainer);

            // 渲染每个卡片
            for (const config of configs) {
                await this.renderCard(gridContainer, config);
            }

            // 开始自动刷新
            if (this.options.autoRefresh) {
                this.startAutoRefresh();
            }
        }

        /**
         * 渲染单个卡片
         * @param {HTMLElement} container 容器
         * @param {Object} config 卡片配置
         */
        async renderCard(container, config) {
            try {
                const renderer = this.renderers.get(config.type);
                if (!renderer) {
                    throw new Error(`未找到类型为 ${config.type} 的渲染器`);
                }

                // 获取数据
                if (config.dataSource) {
                    config.data = await this.dataService.fetchData(config.dataSource);
                }

                // 创建卡片元素
                const cardElement = await renderer.render(config);
                
                // 创建网格项
                const gridItem = this.createGridItem(config);
                gridItem.appendChild(cardElement);
                container.appendChild(gridItem);

                // 存储卡片实例
                this.cards.set(config.id, {
                    config,
                    element: cardElement,
                    gridItem,
                    renderer
                });

                // 触发渲染完成事件
                this.eventBus.emit('card-rendered', { id: config.id, config });

            } catch (error) {
                console.error(`渲染卡片 ${config.id} 失败:`, error);
                this.renderErrorCard(container, config, error);
            }
        }

        /**
         * 更新卡片数据
         * @param {string} cardId 卡片ID
         * @param {Object} data 新数据
         */
        async update(cardId, data) {
            const card = this.cards.get(cardId);
            if (!card) {
                throw new Error(`卡片 ${cardId} 不存在`);
            }

            card.config.data = { ...card.config.data, ...data };
            await card.renderer.update(card.element, card.config);
            
            this.eventBus.emit('card-updated', { id: cardId, data });
        }

        /**
         * 刷新卡片
         * @param {string} cardId 卡片ID，不传则刷新所有
         */
        async refresh(cardId) {
            if (cardId) {
                const card = this.cards.get(cardId);
                if (card && card.config.dataSource) {
                    const newData = await this.dataService.fetchData(card.config.dataSource);
                    await this.update(cardId, newData);
                }
            } else {
                for (const [id, card] of this.cards) {
                    if (card.config.dataSource) {
                        try {
                            const newData = await this.dataService.fetchData(card.config.dataSource);
                            await this.update(id, newData);
                        } catch (error) {
                            console.error(`刷新卡片 ${id} 失败:`, error);
                        }
                    }
                }
            }
        }

        /**
         * 销毁卡片
         * @param {string} cardId 卡片ID
         */
        destroy(cardId) {
            const card = this.cards.get(cardId);
            if (card) {
                card.gridItem.remove();
                this.cards.delete(cardId);
                this.eventBus.emit('card-destroyed', { id: cardId });
            }
        }

        /**
         * 注册渲染器
         * @param {string} type 类型
         * @param {Object} renderer 渲染器
         */
        registerRenderer(type, renderer) {
            this.renderers.set(type, renderer);
        }

        /**
         * 创建网格容器
         */
        createGridContainer(cardCount) {
            const container = document.createElement('div');
            container.className = 'cards-grid';
            return container;
        }

        /**
         * 创建网格项
         */
        createGridItem(config) {
            const item = document.createElement('div');
            item.className = `cards-grid-item ${config.size || 'medium'}`;
            item.dataset.cardId = config.id;
            return item;
        }

        /**
         * 渲染错误卡片
         */
        renderErrorCard(container, config, error) {
            const errorCard = document.createElement('div');
            errorCard.className = 'card card-error';
            errorCard.innerHTML = `
                <div class="card-header">
                    <h4>${config.title || '加载失败'}</h4>
                </div>
                <div class="card-body">
                    <div class="error-message">
                        <i class="fas fa-exclamation-triangle"></i>
                        <span>${error.message}</span>
                    </div>
                </div>
            `;
            
            const gridItem = this.createGridItem(config);
            gridItem.appendChild(errorCard);
            container.appendChild(gridItem);
        }

        /**
         * 开始自动刷新
         */
        startAutoRefresh() {
            if (this.refreshTimer) {
                clearInterval(this.refreshTimer);
            }
            
            this.refreshTimer = setInterval(() => {
                this.refresh();
            }, this.options.refreshInterval);
        }

        /**
         * 停止自动刷新
         */
        stopAutoRefresh() {
            if (this.refreshTimer) {
                clearInterval(this.refreshTimer);
                this.refreshTimer = null;
            }
        }

        /**
         * 加载样式
         */
        loadStyles() {
            if (!document.querySelector('#codespirit-cards-styles')) {
                const link = document.createElement('link');
                link.id = 'codespirit-cards-styles';
                link.rel = 'stylesheet';
                link.href = '/cards-sdk/cards-sdk.css';
                document.head.appendChild(link);
            }
        }
    }

    /**
     * 事件总线
     */
    class EventBus {
        constructor() {
            this.events = new Map();
        }

        on(event, callback) {
            if (!this.events.has(event)) {
                this.events.set(event, []);
            }
            this.events.get(event).push(callback);
        }

        emit(event, data) {
            if (this.events.has(event)) {
                this.events.get(event).forEach(callback => callback(data));
            }
        }

        off(event, callback) {
            if (this.events.has(event)) {
                const callbacks = this.events.get(event);
                const index = callbacks.indexOf(callback);
                if (index > -1) {
                    callbacks.splice(index, 1);
                }
            }
        }
    }

    /**
     * 数据服务
     */
    class DataService {
        constructor(baseUrl) {
            this.baseUrl = baseUrl;
        }

        async fetchData(url, params = {}) {
            const queryString = new URLSearchParams(params).toString();
            const fullUrl = `${this.baseUrl}${url}${queryString ? '?' + queryString : ''}`;
            
            const response = await fetch(fullUrl);
            if (!response.ok) {
                throw new Error(`请求失败: ${response.status}`);
            }
            
            return await response.json();
        }
    }

    /**
     * 统计卡片渲染器
     */
    class StatCardRenderer {
        async render(config) {
            const card = document.createElement('div');
            card.className = `card stat-card theme-${config.style?.theme || 'default'}`;
            
            card.innerHTML = `
                <div class="card-header">
                    ${config.title ? `<h4 class="card-title">${config.title}</h4>` : ''}
                    ${config.subtitle ? `<p class="card-subtitle">${config.subtitle}</p>` : ''}
                </div>
                <div class="card-body">
                    <div class="stat-content">
                        <div class="stat-value">${this.formatValue(config.data.value)}</div>
                        <div class="stat-label">${config.data.label}</div>
                        ${config.data.unit ? `<div class="stat-unit">${config.data.unit}</div>` : ''}
                        ${config.data.trend ? this.renderTrend(config.data.trend) : ''}
                    </div>
                </div>
                ${config.actions ? this.renderActions(config.actions) : ''}
            `;
            
            return card;
        }

        async update(element, config) {
            const valueElement = element.querySelector('.stat-value');
            const labelElement = element.querySelector('.stat-label');
            const trendElement = element.querySelector('.stat-trend');
            
            if (valueElement) valueElement.textContent = this.formatValue(config.data.value);
            if (labelElement) labelElement.textContent = config.data.label;
            
            if (config.data.trend && trendElement) {
                trendElement.outerHTML = this.renderTrend(config.data.trend);
            }
        }

        formatValue(value) {
            if (typeof value === 'number') {
                return value.toLocaleString();
            }
            return value;
        }

        renderTrend(trend) {
            const direction = trend.direction || 'stable';
            const icon = {
                up: 'fas fa-arrow-up',
                down: 'fas fa-arrow-down',
                stable: 'fas fa-minus'
            }[direction];
            
            return `
                <div class="stat-trend trend-${direction}">
                    <i class="${icon}"></i>
                    <span>${trend.value}</span>
                    <small>${trend.period}</small>
                </div>
            `;
        }

        renderActions(actions) {
            return `
                <div class="card-actions">
                    ${actions.map(action => `
                        <button class="btn btn-sm btn-outline-primary" onclick="${action.onclick}">
                            ${action.icon ? `<i class="${action.icon}"></i>` : ''}
                            ${action.label}
                        </button>
                    `).join('')}
                </div>
            `;
        }
    }

    /**
     * 图表卡片渲染器
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
     * 信息卡片渲染器
     */
    class InfoCardRenderer {
        async render(config) {
            const card = document.createElement('div');
            card.className = `card info-card theme-${config.style?.theme || 'default'}`;
            
            card.innerHTML = `
                <div class="card-header">
                    ${config.title ? `<h4 class="card-title">${config.title}</h4>` : ''}
                </div>
                <div class="card-body">
                    <div class="info-content">
                        ${config.data.content || ''}
                    </div>
                </div>
            `;
            
            return card;
        }

        async update(element, config) {
            const contentElement = element.querySelector('.info-content');
            if (contentElement) {
                contentElement.innerHTML = config.data.content || '';
            }
        }
    }

    /**
     * 操作卡片渲染器
     */
    class ActionCardRenderer {
        async render(config) {
            const card = document.createElement('div');
            card.className = `card action-card theme-${config.style?.theme || 'default'}`;
            
            card.innerHTML = `
                <div class="card-header">
                    ${config.title ? `<h4 class="card-title">${config.title}</h4>` : ''}
                </div>
                <div class="card-body">
                    <div class="action-content">
                        ${this.renderActions(config.data.actions || [])}
                    </div>
                </div>
            `;
            
            return card;
        }

        async update(element, config) {
            const contentElement = element.querySelector('.action-content');
            if (contentElement) {
                contentElement.innerHTML = this.renderActions(config.data.actions || []);
            }
        }

        renderActions(actions) {
            return actions.map(action => `
                <button class="btn btn-primary action-btn" onclick="${action.onclick}">
                    ${action.icon ? `<i class="${action.icon}"></i>` : ''}
                    ${action.label}
                </button>
            `).join('');
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
                config: this.getEChartsConfig(chartData, chartType, themeColor),
                // 数据源配置 - 如果有API数据源
                api: chartData.api || undefined,
                // 静态数据
                data: chartData.data || undefined
            };

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
                // 方式1: 使用amis.render渲染Chart组件
                if (window.amis && typeof window.amis.render === 'function') {
                    console.log('使用 amis.render 方法渲染Chart组件');
                    
                    // 清空容器
                    container.innerHTML = '';
                    
                    // 使用Amis render方法
                    const amisInstance = window.amis.render(config, {
                        // 数据上下文
                        data: config.data || {}
                    }, container);
                    
                    console.log('Amis Chart渲染成功:', amisInstance);
                    return amisInstance;
                }
                
                // 方式2: 使用amis.embed方法
                if (window.amis && typeof window.amis.embed === 'function') {
                    console.log('使用 amis.embed 方法');
                    container.innerHTML = '';
                    window.amis.embed(container, config);
                    return;
                }
                
                // 方式3: 通过amisRequire加载
                if (window.amisRequire) {
                    console.log('使用 amisRequire 方法');
                    const amis = window.amisRequire('amis/embed');
                    if (amis && amis.render) {
                        const amisInstance = amis.render(config, {}, container);
                        console.log('Amis图表渲染成功 (amisRequire):', amisInstance);
                        return amisInstance;
                    } else if (amis && amis.embed) {
                        amis.embed(container, config);
                        return;
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

    // 导出模块
    exports.CodeSpiritCardsSDK = CodeSpiritCardsSDK;
    exports.StatCardRenderer = StatCardRenderer;
    exports.ChartCardRenderer = ChartCardRenderer;
    exports.AmisChartCardRenderer = AmisChartCardRenderer;
    exports.InfoCardRenderer = InfoCardRenderer;
    exports.ActionCardRenderer = ActionCardRenderer;

    // 全局访问
    if (typeof window !== 'undefined') {
        window.CodeSpiritCards = window.CodeSpiritCards || {};
        window.CodeSpiritCards.SDK = CodeSpiritCardsSDK;
        window.CodeSpiritCards.StatCardRenderer = StatCardRenderer;
        window.CodeSpiritCards.ChartCardRenderer = ChartCardRenderer;
        window.CodeSpiritCards.AmisChartCardRenderer = AmisChartCardRenderer;
        window.CodeSpiritCards.InfoCardRenderer = InfoCardRenderer;
        window.CodeSpiritCards.ActionCardRenderer = ActionCardRenderer;
    }

}))); 