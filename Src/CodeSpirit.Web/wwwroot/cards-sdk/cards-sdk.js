/**
 * CodeSpirit 统计卡片前端SDK
 * @version 1.0.3
 * @author CodeSpirit Team
 * 
 * 修复说明 v1.0.3:
 * - 图表渲染器已剥离到独立的 chart-renderers.js 文件
 * - 表格渲染器已剥离到独立的 table-card-renderer.js 文件
 * - 核心SDK只保留基础渲染器和管理功能
 * - 支持智能延迟注册机制
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
                // 可选渲染器配置
                enableTableRenderer: options.enableTableRenderer !== false, // 默认启用
                enableChartRenderers: options.enableChartRenderers !== false, // 默认启用
                silentMode: options.silentMode || false, // 静默模式，减少警告日志
                ...options
            };
            
            this.cards = new Map();
            this.renderers = new Map();
            this.eventBus = new EventBus();
            this.dataService = new DataService(this.options.baseUrl);
            
            // 全局AMIS配置
            this.globalAmisConfig = {
                theme: 'antd',
                locale: 'zh-CN',
                debug: true,
                apiBaseUrl: this.options.baseUrl,
                // 全局请求拦截器
                requestInterceptor: null,
                // 全局响应拦截器  
                responseInterceptor: null,
                // 全局错误处理器
                errorHandler: null
            };
            
            this.init();
        }

        /**
         * 初始化SDK
         */
        init() {
            // 检查TokenManager依赖 - 必须存在
            if (!window.TokenManager) {
                const errorMsg = 'TokenManager is required but not found. Please ensure TokenManager.js is loaded before Cards SDK.';
                console.error('❌', errorMsg);
                throw new Error(errorMsg);
            }
            
            const platformType = window.TokenManager.platformType;
            const platformName = window.TokenManager.getClientTypeName ? 
                window.TokenManager.getClientTypeName() : '系统';
            console.log(`🔐 TokenManager已就绪 - 平台类型: ${platformType} (${platformName})`);
            
            // 输出当前认证状态
            if (window.TokenManager.isAuthenticated && window.TokenManager.isAuthenticated()) {
                console.log('✅ 用户已认证');
                const platformInfo = window.TokenManager.getPlatformInfo();
                if (platformInfo) {
                    console.log('📋 平台信息:', platformInfo);
                }
            } else {
                console.warn('⚠️ 用户未认证或Token已过期');
            }
            
            // 注册默认渲染器
            this.registerRenderer('stat', new StatCardRenderer());
            this.registerRenderer('info', new InfoCardRenderer());
            this.registerRenderer('action', new ActionCardRenderer());
            
            // 根据配置决定是否启用可选渲染器
            if (this.options.enableTableRenderer) {
                this.registerTableRenderer();
            }
            
            if (this.options.enableChartRenderers) {
                this.registerChartRenderers();
            }
            
            // 初始化样式
            this.loadStyles();
            this.loadTableStyles();
            window.amis = amisRequire('amis/embed');
            console.log('✅ CodeSpirit Cards SDK 初始化完成');
        }

        /**
         * 加载表格样式
         */
        loadTableStyles() {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/cards-sdk/table-card.css';
            document.head.appendChild(link);
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
                        const newData = await this.dataService.fetchData(card.config.dataSource);
                        await this.update(id, newData);
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
         * @param {string} type 渲染器类型
         * @param {Object} renderer 渲染器实例
         */
        registerRenderer(type, renderer) {
            this.renderers.set(type, renderer);
            console.log(`✅ 渲染器 ${type} 注册成功`);
        }

        /**
         * 注册表格渲染器
         * 支持延迟注册机制，确保table-card-renderer.js正确加载
         */
        registerTableRenderer() {
            // 检查TableCardRenderer是否已加载
            if (typeof window.TableCardRenderer !== 'undefined') {
                console.log('✅ 发现TableCardRenderer，立即注册');
                this.registerRenderer('table', new window.TableCardRenderer());
                return;
            }
            
            // 延迟注册机制 - 等待table-card-renderer.js加载
            console.log('⏳ TableCardRenderer未就绪，启动延迟注册机制...');
            
            let attempts = 0;
            const maxAttempts = 50; // 5秒超时
            const checkInterval = 100; // 100ms间隔
            
            const checkAndRegister = () => {
                attempts++;
                
                if (typeof window.TableCardRenderer !== 'undefined') {
                    console.log(`✅ TableCardRenderer已加载，延迟注册成功 (尝试 ${attempts}/${maxAttempts})`);
                    this.registerRenderer('table', new window.TableCardRenderer());
                    return;
                }
                
                if (attempts >= maxAttempts) {
                    if (!this.options.silentMode) {
                        console.warn(`⚠️ TableCardRenderer注册超时 (${attempts}次尝试)`);
                        console.warn('请确保table-card-renderer.js在cards-sdk.js之后正确加载');
                    }
                    return;
                }
                
                setTimeout(checkAndRegister, checkInterval);
            };
            
            checkAndRegister();
        }

        /**
         * 注册图表渲染器
         * 支持延迟注册机制，确保chart-renderers.js正确加载
         */
        registerChartRenderers() {
            // 检查图表渲染器是否已加载
            const hasChartRenderer = typeof window.ChartCardRenderer !== 'undefined';
            const hasAmisChartRenderer = typeof window.AmisChartCardRenderer !== 'undefined';
            
            if (hasChartRenderer && hasAmisChartRenderer) {
                console.log('✅ 发现图表渲染器，立即注册');
                this.registerRenderer('chart', new window.ChartCardRenderer());
                this.registerRenderer('amis-chart', new window.AmisChartCardRenderer());
                return;
            }
            
            // 延迟注册机制 - 等待chart-renderers.js加载
            console.log('⏳ 图表渲染器未就绪，启动延迟注册机制...');
            
            let attempts = 0;
            const maxAttempts = 50; // 5秒超时
            const checkInterval = 100; // 100ms间隔
            
            const checkAndRegister = () => {
                attempts++;
                
                const chartReady = typeof window.ChartCardRenderer !== 'undefined';
                const amisChartReady = typeof window.AmisChartCardRenderer !== 'undefined';
                
                if (chartReady && amisChartReady) {
                    console.log(`✅ 图表渲染器已加载，延迟注册成功 (尝试 ${attempts}/${maxAttempts})`);
                    this.registerRenderer('chart', new window.ChartCardRenderer());
                    this.registerRenderer('amis-chart', new window.AmisChartCardRenderer());
                    return;
                }
                
                if (attempts >= maxAttempts) {
                    if (!this.options.silentMode) {
                        console.warn(`⚠️ 图表渲染器注册超时 (${attempts}次尝试)`);
                        console.warn('请确保chart-renderers.js在cards-sdk.js之后正确加载');
                        console.warn(`状态: ChartCardRenderer=${chartReady}, AmisChartCardRenderer=${amisChartReady}`);
                    }
                    return;
                }
                
                setTimeout(checkAndRegister, checkInterval);
            };
            
            checkAndRegister();
        }

        /**
         * 设置全局AMIS配置
         * @param {Object} config AMIS配置
         */
        setGlobalAmisConfig(config) {
            Object.assign(this.globalAmisConfig, config);
            console.log('🔧 更新全局AMIS配置:', this.globalAmisConfig);
        }

        /**
         * 获取全局AMIS配置
         * @returns {Object} 全局AMIS配置
         */
        getGlobalAmisConfig() {
            return { ...this.globalAmisConfig };
        }

        /**
         * 设置全局请求拦截器
         * @param {Function} interceptor 拦截器函数
         */
        setRequestInterceptor(interceptor) {
            this.globalAmisConfig.requestInterceptor = interceptor;
            console.log('🔧 设置全局请求拦截器');
        }

        /**
         * 设置全局响应拦截器
         * @param {Function} interceptor 拦截器函数
         */
        setResponseInterceptor(interceptor) {
            this.globalAmisConfig.responseInterceptor = interceptor;
            console.log('🔧 设置全局响应拦截器');
        }

        /**
         * 设置全局错误处理器
         * @param {Function} handler 错误处理函数
         */
        setErrorHandler(handler) {
            this.globalAmisConfig.errorHandler = handler;
            console.log('🔧 设置全局错误处理器');
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
            
            // 构建请求头 - 必须使用TokenManager
            if (!window.TokenManager) {
                throw new Error('TokenManager is required but not found. Cannot make authenticated requests.');
            }
            
            const authHeaders = window.TokenManager.getAuthHeaders();
            
            const response = await fetch(fullUrl, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...authHeaders
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
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
                        ${config.data.unit ? `<div class="stat-unit">${config.data.unit}</div>` : ''}
                        ${config.data.trend ? this.renderTrend(config.data.trend) : ''}
                    </div>
                    <div class="stat-actions">
                        ${config.actions ? this.renderActions(config.actions) : ''}
                    </div>
                </div>
            `;
            
            return card;
        }

        async update(element, config) {
            const valueElement = element.querySelector('.stat-value');
            if (valueElement) {
                valueElement.textContent = this.formatValue(config.data.value);
            }
        }

        formatValue(value) {
            if (typeof value === 'number') {
                return value.toLocaleString();
            }
            return value;
        }

        renderTrend(trend) {
            const trendClass = trend > 0 ? 'trend-up' : trend < 0 ? 'trend-down' : 'trend-stable';
            const trendIcon = trend > 0 ? '↗' : trend < 0 ? '↘' : '→';
            return `
                <div class="stat-trend ${trendClass}">
                    <span class="trend-icon">${trendIcon}</span>
                    <span class="trend-value">${Math.abs(trend)}%</span>
                </div>
            `;
        }

        renderActions(actions) {
            return actions.map(action => `
                <button class="btn btn-sm btn-outline-primary" onclick="${action.onclick}">
                    ${action.icon ? `<i class="${action.icon}"></i>` : ''}
                    ${action.label}
                </button>
            `).join('');
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

    // 图表渲染器已移至独立文件 chart-renderers.js
    // 表格渲染器已移至独立文件 table-card-renderer.js
    // 避免代码重复，提高模块化程度

    // 导出模块
    exports.CodeSpiritCardsSDK = CodeSpiritCardsSDK;
    exports.StatCardRenderer = StatCardRenderer;
    exports.InfoCardRenderer = InfoCardRenderer;
    exports.ActionCardRenderer = ActionCardRenderer;
    // ChartCardRenderer 和 AmisChartCardRenderer 现在从独立的 chart-renderers.js 文件中导出
    // TableCardRenderer 现在从独立的 table-card-renderer.js 文件中导出

    // 全局访问
    if (typeof window !== 'undefined') {
        window.CodeSpiritCards = window.CodeSpiritCards || {};
        window.CodeSpiritCards.SDK = CodeSpiritCardsSDK;
        window.CodeSpiritCards.StatCardRenderer = StatCardRenderer;
        window.CodeSpiritCards.InfoCardRenderer = InfoCardRenderer;
        window.CodeSpiritCards.ActionCardRenderer = ActionCardRenderer;
        // ChartCardRenderer 和 AmisChartCardRenderer 现在从独立的 chart-renderers.js 文件中注册
        // TableCardRenderer 现在从独立的 table-card-renderer.js 文件中注册
    }

}))); 