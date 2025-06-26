/**
 * CodeSpirit Cards SDK - AMIS集成适配器
 * 用于将Cards SDK与现有的AMIS系统集成
 * @version 1.0.1 - 修复潜在问题
 */
(function(global) {
    'use strict';
    
    // 🔧 修复1: 改进初始化时机判断，不直接退出
    const isAmisAvailable = () => {
        return typeof global.amisRequire !== 'undefined' || typeof global.amis !== 'undefined';
    };
    
    // 🔧 修复2: 添加延迟初始化机制
    const delayedInitCallbacks = [];
    const addDelayedInitCallback = (callback) => {
        if (isAmisAvailable()) {
            callback();
        } else {
            delayedInitCallbacks.push(callback);
        }
    };

    /**
     * AMIS Cards适配器类
     */
    class AmisCardsAdapter {
        constructor(options = {}) {
            this.options = {
                amisScope: global.amisScope || global.amis,
                cardsSDK: null,
                maxRetries: 3, // 🔧 添加重试机制
                retryDelay: 1000,
                ...options
            };
            
            this.initAttempts = 0;
            this.initialized = false;
            this.init();
        }

        /**
         * 初始化适配器
         */
        async init() {
            if (this.initialized) {
                return;
            }
            
            try {
                // 🔧 修复3: 改进Cards SDK依赖检查
                if (typeof CodeSpiritCards === 'undefined') {
                    if (this.initAttempts < this.options.maxRetries) {
                        this.initAttempts++;
                        console.warn(`CodeSpirit Cards SDK未找到，第${this.initAttempts}次重试...`);
                        setTimeout(() => this.init(), this.options.retryDelay);
                        return;
                    }
                    throw new Error('CodeSpirit Cards SDK未找到，已达到最大重试次数');
                }
                
                this.cardsSDK = new CodeSpiritCards.SDK(this.options);
                
                // 🔧 修复4: 异步注册渲染器
                await this.registerAmisRenderer();
                
                this.initialized = true;
                console.log('AMIS Cards适配器初始化完成');
            } catch (error) {
                console.error('AMIS Cards适配器初始化失败:', error);
                throw error;
            }
        }

        /**
         * 注册AMIS渲染器
         */
        async registerAmisRenderer() {
            // 🔧 修复5: 更完善的Amis可用性检查
            if (!isAmisAvailable()) {
                console.warn('AMIS未找到，跳过渲染器注册');
                return;
            }

            // 使用 amisRequire 获取 Amis 组件（推荐方式）
            if (typeof global.amisRequire !== 'undefined') {
                try {
                    const React = global.amisRequire('react');
                    const amisCore = global.amisRequire('@fex/amis-core');
                    
                    if (amisCore && amisCore.Renderer && React) {
                        await this.registerWithAmisCore(amisCore, React);
                        return;
                    }
                } catch (error) {
                    console.warn('amisRequire 方式注册失败，尝试备用方式:', error);
                }
            }

            // 备用方式：使用全局 amis 对象
            if (typeof global.amis !== 'undefined') {
                await this.registerWithAmisGlobal(global.amis);
                return;
            }

            console.warn('无法找到合适的 Amis 注册方式');
        }

        /**
         * 使用 amisCore 注册渲染器（推荐方式）
         */
        async registerWithAmisCore(amisCore, React) {
            console.log('开始注册AmisCore渲染器');

            // 🔧 修复6: 改进React组件实现
            // 注册统计卡片渲染器
            amisCore.Renderer({
                type: 'codespirit-cards',
                autoVar: true
            })(class extends React.Component {
                static displayName = 'CodeSpiritCards';

                constructor(props) {
                    super(props);
                    this.state = {
                        isRendering: false,
                        renderError: null
                    };
                    this.cardsContainer = null;
                    this.cardInstances = new Map();
                    // 🔧 修复7: 绑定方法上下文
                    this.renderCards = this.renderCards.bind(this);
                    this.destroyCards = this.destroyCards.bind(this);
                }

                async componentDidMount() {
                    // 🔧 修复8: 添加错误处理
                    try {
                        await this.renderCards();
                    } catch (error) {
                        console.error('componentDidMount渲染失败:', error);
                        this.setState({ renderError: error });
                    }
                }

                async componentDidUpdate(prevProps) {
                    // 🔧 修复9: 改进更新条件判断，避免无限循环
                    const cardsChanged = JSON.stringify(prevProps.cards) !== JSON.stringify(this.props.cards);
                    const dataChanged = JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data);
                    
                    if ((cardsChanged || dataChanged) && !this.state.isRendering) {
                        try {
                            await this.renderCards();
                        } catch (error) {
                            console.error('componentDidUpdate渲染失败:', error);
                            this.setState({ renderError: error });
                        }
                    }
                }

                componentWillUnmount() {
                    // 🔧 修复10: 安全的清理
                    try {
                        this.destroyCards();
                    } catch (error) {
                        console.error('组件清理失败:', error);
                    }
                }

                async renderCards() {
                    const { cards, data } = this.props;
                    
                    if (!cards || !Array.isArray(cards)) {
                        return;
                    }

                    // 🔧 修复11: 防止并发渲染
                    if (this.state.isRendering) {
                        return;
                    }

                    this.setState({ isRendering: true, renderError: null });

                    try {
                        // 处理卡片配置
                        const processedCards = this.processCardConfigs(cards, data);
                        
                        // 渲染到容器
                        if (this.cardsContainer && window.amisCardsAdapter?.cardsSDK) {
                            await window.amisCardsAdapter.cardsSDK.render(this.cardsContainer, processedCards);
                        }
                    } catch (error) {
                        console.error('渲染Cards失败:', error);
                        this.setState({ renderError: error });
                    } finally {
                        this.setState({ isRendering: false });
                    }
                }

                processCardConfigs(cards, data) {
                    return cards.map((card, index) => {
                        // 处理数据绑定
                        const processedCard = { ...card };
                        
                        // 生成唯一ID
                        if (!processedCard.id) {
                            processedCard.id = `card-${index}-${Date.now()}`;
                        }

                        // 🔧 修复12: 安全的数据模板替换
                        if (processedCard.data && data) {
                            processedCard.data = this.safeReplaceDataTemplates(processedCard.data, data);
                        }

                        // API路径处理
                        if (processedCard.dataSource) {
                            processedCard.dataSource = this.resolveApiPath(processedCard.dataSource, data);
                        }

                        return processedCard;
                    });
                }

                // 🔧 修复13: 防止循环引用的安全模板替换
                safeReplaceDataTemplates(cardData, contextData, depth = 0) {
                    // 防止深度递归
                    if (depth > 10) {
                        console.warn('模板替换深度过深，停止递归');
                        return cardData;
                    }
                    
                    // 防止循环引用
                    if (typeof cardData !== 'object' || cardData === null) {
                        return cardData;
                    }
                    
                    // 检查是否为循环引用
                    if (cardData === contextData) {
                        console.warn('检测到循环引用，跳过处理');
                        return cardData;
                    }
                    
                    const result = Array.isArray(cardData) ? [] : {};
                    
                    Object.keys(cardData).forEach(key => {
                        const value = cardData[key];
                        
                        if (typeof value === 'string' && value.includes('${')) {
                            result[key] = value.replace(/\$\{(\w+)\}/g, (match, prop) => {
                                return contextData[prop] || match;
                            });
                        } else if (typeof value === 'object' && value !== null) {
                            result[key] = this.safeReplaceDataTemplates(value, contextData, depth + 1);
                        } else {
                            result[key] = value;
                        }
                    });
                    
                    return result;
                }

                resolveApiPath(path, data) {
                    // 解析API路径中的变量
                    return path.replace(/\$\{(\w+)\}/g, (match, prop) => {
                        return data[prop] || match;
                    });
                }

                // 🔧 修复14: 安全的实例销毁
                destroyCards() {
                    try {
                        if (window.amisCardsAdapter?.cardsSDK && this.cardInstances.size > 0) {
                            this.cardInstances.forEach((card, id) => {
                                try {
                                    window.amisCardsAdapter.cardsSDK.destroy(id);
                                } catch (error) {
                                    console.warn(`销毁卡片${id}失败:`, error);
                                }
                            });
                        }
                        this.cardInstances.clear();
                    } catch (error) {
                        console.error('销毁卡片实例失败:', error);
                    }
                }

                render() {
                    const { className, style } = this.props;
                    const { renderError } = this.state;
                    
                    // 🔧 修复15: 添加错误状态渲染
                    if (renderError) {
                        return React.createElement('div', {
                            className: `codespirit-cards-error ${className || ''}`,
                            style: style || {}
                        }, `渲染失败: ${renderError.message}`);
                    }
                    
                    return React.createElement('div', {
                        ref: (ref) => { this.cardsContainer = ref; },
                        className: `codespirit-cards-container ${className || ''}`,
                        style: style || {}
                    });
                }
            });

            // 🔧 修复16: 改进单个卡片渲染器
            // 注册单个卡片渲染器
            amisCore.Renderer({
                type: 'codespirit-stat-card'
            })(class extends React.Component {
                static displayName = 'CodeSpiritStatCard';

                constructor(props) {
                    super(props);
                    this.state = {
                        isRendering: false,
                        renderError: null
                    };
                    this.cardRef = null;
                    this.renderStatCard = this.renderStatCard.bind(this);
                }

                async componentDidMount() {
                    try {
                        await this.renderStatCard();
                    } catch (error) {
                        console.error('统计卡片componentDidMount失败:', error);
                        this.setState({ renderError: error });
                    }
                }

                async componentDidUpdate(prevProps) {
                    if (JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data) && 
                        !this.state.isRendering) {
                        try {
                            await this.renderStatCard();
                        } catch (error) {
                            console.error('统计卡片更新失败:', error);
                            this.setState({ renderError: error });
                        }
                    }
                }

                async renderStatCard() {
                    const { data, title, subtitle, theme, size } = this.props;
                    
                    if (!this.cardRef || !data || this.state.isRendering) return;

                    this.setState({ isRendering: true, renderError: null });

                    try {
                        const cardConfig = {
                            id: `stat-card-${Date.now()}`,
                            type: 'stat',
                            title: title || '统计卡片',
                            subtitle: subtitle,
                            size: size || 'medium',
                            style: { theme: theme || 'default' },
                            data: {
                                value: data.value || 0,
                                label: data.label || '',
                                unit: data.unit,
                                trend: data.trend
                            }
                        };

                        if (!CodeSpiritCards?.StatCardRenderer) {
                            throw new Error('StatCardRenderer未找到');
                        }

                        const renderer = new CodeSpiritCards.StatCardRenderer();
                        const cardElement = await renderer.render(cardConfig);
                        
                        // 清空容器并添加新卡片
                        this.cardRef.innerHTML = '';
                        this.cardRef.appendChild(cardElement);
                    } catch (error) {
                        console.error('渲染统计卡片失败:', error);
                        this.setState({ renderError: error });
                    } finally {
                        this.setState({ isRendering: false });
                    }
                }

                render() {
                    const { className, style } = this.props;
                    const { renderError } = this.state;
                    
                    if (renderError) {
                        return React.createElement('div', {
                            className: `codespirit-stat-card-error ${className || ''}`,
                            style: style || {}
                        }, `统计卡片渲染失败: ${renderError.message}`);
                    }
                    
                    return React.createElement('div', {
                        ref: (ref) => { this.cardRef = ref; },
                        className: `codespirit-stat-card-wrapper ${className || ''}`,
                        style: style || {}
                    });
                }
            });

            // 🔧 修复17: 改进Amis Chart卡片渲染器
            // 注册Amis Chart卡片渲染器（直接使用）
            amisCore.Renderer({
                type: 'codespirit-amis-chart'
            })(class extends React.Component {
                static displayName = 'CodeSpiritAmisChart';

                constructor(props) {
                    super(props);
                    this.state = {
                        isRendering: false,
                        renderError: null
                    };
                    this.chartRef = null;
                    this.renderAmisChart = this.renderAmisChart.bind(this);
                }

                async componentDidMount() {
                    try {
                        await this.renderAmisChart();
                    } catch (error) {
                        console.error('Amis图表componentDidMount失败:', error);
                        this.setState({ renderError: error });
                    }
                }

                async componentDidUpdate(prevProps) {
                    const dataChanged = JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data);
                    const chartTypeChanged = prevProps.chartType !== this.props.chartType;
                    
                    if ((dataChanged || chartTypeChanged) && !this.state.isRendering) {
                        try {
                            await this.renderAmisChart();
                        } catch (error) {
                            console.error('Amis图表更新失败:', error);
                            this.setState({ renderError: error });
                        }
                    }
                }

                async renderAmisChart() {
                    const { data, title, subtitle, chartType, height, theme } = this.props;
                    
                    if (!this.chartRef || !data || this.state.isRendering) return;

                    this.setState({ isRendering: true, renderError: null });

                    try {
                        const cardConfig = {
                            id: `amis-chart-${Date.now()}`,
                            type: 'amis-chart',
                            title: title || 'Amis图表',
                            subtitle: subtitle,
                            style: { 
                                height: height || 300, 
                                theme: theme || 'default' 
                            },
                            data: data
                        };

                        if (!CodeSpiritCards?.AmisChartCardRenderer) {
                            throw new Error('AmisChartCardRenderer未找到');
                        }

                        const renderer = new CodeSpiritCards.AmisChartCardRenderer();
                        const cardElement = await renderer.render(cardConfig);
                        
                        // 清空容器并添加新卡片
                        this.chartRef.innerHTML = '';
                        this.chartRef.appendChild(cardElement);
                    } catch (error) {
                        console.error('渲染Amis图表卡片失败:', error);
                        this.setState({ renderError: error });
                    } finally {
                        this.setState({ isRendering: false });
                    }
                }

                render() {
                    const { className, style } = this.props;
                    const { renderError } = this.state;
                    
                    if (renderError) {
                        return React.createElement('div', {
                            className: `codespirit-amis-chart-error ${className || ''}`,
                            style: style || {}
                        }, `Amis图表渲染失败: ${renderError.message}`);
                    }
                    
                    return React.createElement('div', {
                        ref: (ref) => { this.chartRef = ref; },
                        className: `codespirit-amis-chart-wrapper ${className || ''}`,
                        style: style || {}
                    });
                }
            });

            console.log('AMIS Cards渲染器注册完成（amisCore方式）');
        }

        /**
         * 使用全局 amis 对象注册渲染器（备用方式）
         */
        async registerWithAmisGlobal(amis) {
            if (!amis.Renderer) {
                console.warn('amis.Renderer 方法不存在，跳过渲染器注册');
                return;
            }

            try {
                // 🔧 修复18: 改进React依赖检查
                const React = window.React || amis.React || global.React;
                
                if (!React) {
                    console.warn('React 未找到，无法注册渲染器');
                    return;
                }

                // 🔧 修复19: 使用与amisCore相同的组件实现
                // 注册统计卡片渲染器
                amis.Renderer({
                    type: 'codespirit-cards',
                    autoVar: true
                })(class extends React.Component {
                    static displayName = 'CodeSpiritCards';

                    constructor(props) {
                        super(props);
                        this.state = {
                            isRendering: false,
                            renderError: null
                        };
                        this.cardsContainer = null;
                        this.cardInstances = new Map();
                        this.renderCards = this.renderCards.bind(this);
                        this.destroyCards = this.destroyCards.bind(this);
                    }

                    async componentDidMount() {
                        try {
                            await this.renderCards();
                        } catch (error) {
                            console.error('全局amis组件componentDidMount失败:', error);
                            this.setState({ renderError: error });
                        }
                    }

                    async componentDidUpdate(prevProps) {
                        const cardsChanged = JSON.stringify(prevProps.cards) !== JSON.stringify(this.props.cards);
                        const dataChanged = JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data);
                        
                        if ((cardsChanged || dataChanged) && !this.state.isRendering) {
                            try {
                                await this.renderCards();
                            } catch (error) {
                                console.error('全局amis组件更新失败:', error);
                                this.setState({ renderError: error });
                            }
                        }
                    }

                    componentWillUnmount() {
                        try {
                            this.destroyCards();
                        } catch (error) {
                            console.error('全局amis组件清理失败:', error);
                        }
                    }

                    async renderCards() {
                        const { cards, data } = this.props;
                        
                        if (!cards || !Array.isArray(cards) || this.state.isRendering) {
                            return;
                        }

                        this.setState({ isRendering: true, renderError: null });

                        try {
                            const processedCards = this.processCardConfigs(cards, data);
                            
                            if (this.cardsContainer && window.amisCardsAdapter?.cardsSDK) {
                                await window.amisCardsAdapter.cardsSDK.render(this.cardsContainer, processedCards);
                            }
                        } catch (error) {
                            console.error('全局amis方式渲染Cards失败:', error);
                            this.setState({ renderError: error });
                        } finally {
                            this.setState({ isRendering: false });
                        }
                    }

                    processCardConfigs(cards, data) {
                        return cards.map((card, index) => {
                            const processedCard = { ...card };
                            
                            if (!processedCard.id) {
                                processedCard.id = `global-card-${index}-${Date.now()}`;
                            }

                            if (processedCard.data && data) {
                                processedCard.data = this.safeReplaceDataTemplates(processedCard.data, data);
                            }

                            if (processedCard.dataSource) {
                                processedCard.dataSource = this.resolveApiPath(processedCard.dataSource, data);
                            }

                            return processedCard;
                        });
                    }

                    safeReplaceDataTemplates(cardData, contextData, depth = 0) {
                        if (depth > 10) {
                            console.warn('全局amis模板替换深度过深，停止递归');
                            return cardData;
                        }
                        
                        if (typeof cardData !== 'object' || cardData === null) {
                            return cardData;
                        }
                        
                        if (cardData === contextData) {
                            console.warn('全局amis检测到循环引用，跳过处理');
                            return cardData;
                        }
                        
                        const result = Array.isArray(cardData) ? [] : {};
                        
                        Object.keys(cardData).forEach(key => {
                            const value = cardData[key];
                            
                            if (typeof value === 'string' && value.includes('${')) {
                                result[key] = value.replace(/\$\{(\w+)\}/g, (match, prop) => {
                                    return contextData[prop] || match;
                                });
                            } else if (typeof value === 'object' && value !== null) {
                                result[key] = this.safeReplaceDataTemplates(value, contextData, depth + 1);
                            } else {
                                result[key] = value;
                            }
                        });
                        
                        return result;
                    }

                    resolveApiPath(path, data) {
                        return path.replace(/\$\{(\w+)\}/g, (match, prop) => {
                            return data[prop] || match;
                        });
                    }

                    destroyCards() {
                        try {
                            if (window.amisCardsAdapter?.cardsSDK && this.cardInstances.size > 0) {
                                this.cardInstances.forEach((card, id) => {
                                    try {
                                        window.amisCardsAdapter.cardsSDK.destroy(id);
                                    } catch (error) {
                                        console.warn(`全局amis销毁卡片${id}失败:`, error);
                                    }
                                });
                            }
                            this.cardInstances.clear();
                        } catch (error) {
                            console.error('全局amis销毁卡片实例失败:', error);
                        }
                    }

                    render() {
                        const { className, style } = this.props;
                        const { renderError } = this.state;
                        
                        if (renderError) {
                            return React.createElement('div', {
                                className: `codespirit-cards-error ${className || ''}`,
                                style: style || {}
                            }, `全局amis渲染失败: ${renderError.message}`);
                        }
                        
                        return React.createElement('div', {
                            ref: (ref) => { this.cardsContainer = ref; },
                            className: `codespirit-cards-container ${className || ''}`,
                            style: style || {}
                        });
                    }
                });

                console.log('AMIS Cards渲染器注册完成（全局amis方式）');
            } catch (error) {
                console.error('注册AMIS渲染器失败:', error);
                throw error;
            }
        }

        /**
         * 生成AMIS配置
         */
        generateAmisConfig(cards) {
            return {
                type: 'page',
                title: '统计卡片仪表板',
                body: [
                    {
                        type: 'codespirit-cards',
                        cards: cards,
                        className: 'cards-dashboard'
                    }
                ]
            };
        }

        /**
         * 生成统计页面配置（兼容现有StatisticsConfigBuilder）
         */
        generateStatisticsPageConfig(controllerName, cards) {
            return {
                type: 'page',
                title: `${controllerName} 统计`,
                body: [
                    {
                        type: 'form',
                        title: '查询条件',
                        mode: 'inline',
                        body: [
                            {
                                type: 'input-date-range',
                                name: 'dateRange',
                                label: '时间范围',
                                format: 'YYYY-MM-DD',
                                value: '-30days,today'
                            }
                        ],
                        actions: [],
                        submitOnChange: true
                    },
                    {
                        type: 'codespirit-cards',
                        cards: cards,
                        data: '${dateRange}',
                        className: 'statistics-cards-grid'
                    }
                ]
            };
        }
    }

    /**
     * 工具函数
     */
    const AmisCardsUtils = {
        /**
         * 将现有的统计配置转换为Cards配置
         */
        convertStatisticsToCards(statisticsConfig) {
            const cards = [];
            
            if (statisticsConfig.body && Array.isArray(statisticsConfig.body)) {
                statisticsConfig.body.forEach((item, index) => {
                    if (item.type === 'grid' && item.columns) {
                        item.columns.forEach((column, colIndex) => {
                            if (column.body && column.body.type === 'card') {
                                cards.push({
                                    id: `converted-card-${index}-${colIndex}`,
                                    type: 'stat',
                                    title: column.body.header?.title || '统计项',
                                    size: this.mapGridSizeToCardSize(column.md || 6),
                                    dataSource: column.body.api || column.body.source,
                                    style: { 
                                        theme: this.mapClassNameToTheme(column.body.className) 
                                    }
                                });
                            }
                        });
                    }
                });
            }
            
            return cards;
        },

        mapGridSizeToCardSize(mdSize) {
            if (mdSize <= 4) return 'small';
            if (mdSize <= 8) return 'medium';
            return 'large';
        },

        mapClassNameToTheme(className) {
            if (!className) return 'default';
            if (className.includes('primary')) return 'primary';
            if (className.includes('success')) return 'success';
            if (className.includes('warning')) return 'warning';
            if (className.includes('danger')) return 'danger';
            return 'default';
        }
    };

    // 🔧 修复20: 改进全局暴露和初始化逻辑
    // 全局暴露
    if (typeof window !== 'undefined') {
        window.AmisCardsAdapter = AmisCardsAdapter;
        window.AmisCardsUtils = AmisCardsUtils;
        
        // 触发延迟初始化回调
        if (isAmisAvailable()) {
            delayedInitCallbacks.forEach(callback => {
                try {
                    callback();
                } catch (error) {
                    console.error('延迟初始化回调执行失败:', error);
                }
            });
            delayedInitCallbacks.length = 0;
        }
    }

    // 🔧 修复21: 改进初始化策略
    const initializeAdapter = () => {
        if (typeof window === 'undefined') {
            return;
        }

        try {
            // 检查必要条件
            const hasCodeSpiritCards = typeof CodeSpiritCards !== 'undefined';
            const hasAmis = isAmisAvailable();
            
            console.log('AmisCardsAdapter 初始化条件检查:', {
                hasCodeSpiritCards,
                hasAmis,
                hasWindow: typeof window !== 'undefined'
            });

            if (hasCodeSpiritCards && hasAmis) {
                // 条件满足，立即初始化
                window.amisCardsAdapter = new AmisCardsAdapter();
                console.log('✅ AmisCardsAdapter 初始化成功');
            } else {
                // 条件不满足，设置延迟初始化
                const missingDeps = [];
                if (!hasCodeSpiritCards) missingDeps.push('CodeSpiritCards');
                if (!hasAmis) missingDeps.push('Amis');
                
                console.warn(`⚠️ AmisCardsAdapter: 缺少依赖 [${missingDeps.join(', ')}]，添加到延迟初始化队列`);
                
                addDelayedInitCallback(() => {
                    if (!window.amisCardsAdapter) {
                        window.amisCardsAdapter = new AmisCardsAdapter();
                        console.log('✅ AmisCardsAdapter 延迟初始化成功');
                    }
                });
                
                // 设置轮询检查机制，最多检查10次
                let attempts = 0;
                const maxAttempts = 10;
                const checkInterval = 1000; // 1秒
                
                const pollForDependencies = () => {
                    attempts++;
                    const nowHasCodeSpiritCards = typeof CodeSpiritCards !== 'undefined';
                    const nowHasAmis = isAmisAvailable();
                    
                    if (nowHasCodeSpiritCards && nowHasAmis && !window.amisCardsAdapter) {
                        try {
                            window.amisCardsAdapter = new AmisCardsAdapter();
                            console.log(`✅ AmisCardsAdapter 轮询初始化成功 (第${attempts}次尝试)`);
                        } catch (error) {
                            console.error(`❌ AmisCardsAdapter 轮询初始化失败 (第${attempts}次尝试):`, error);
                        }
                    } else if (attempts < maxAttempts) {
                        setTimeout(pollForDependencies, checkInterval);
                    } else {
                        console.warn(`⚠️ AmisCardsAdapter: 已达到最大尝试次数 (${maxAttempts})，停止轮询`);
                    }
                };
                
                // 如果有任何依赖缺失，开始轮询
                if (!hasCodeSpiritCards || !hasAmis) {
                    setTimeout(pollForDependencies, checkInterval);
                }
            }
        } catch (error) {
            console.error('❌ AmisCardsAdapter 初始化失败:', error);
        }
    };

    // 🔧 修复22: 确保在合适的时机初始化
    if (typeof window !== 'undefined') {
        if (document.readyState === 'loading') {
            // 文档还在加载中，等待DOM加载完成
            document.addEventListener('DOMContentLoaded', initializeAdapter);
        } else {
            // 文档已加载完成，立即初始化
            initializeAdapter();
        }
        
        // 也监听window.load事件作为备用
        window.addEventListener('load', () => {
            if (!window.amisCardsAdapter) {
                console.log('window.load 事件触发，尝试初始化 AmisCardsAdapter');
                initializeAdapter();
            }
        });
    }

})(typeof window !== 'undefined' ? window : global); 