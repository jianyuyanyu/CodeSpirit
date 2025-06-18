/**
 * CodeSpirit Cards SDK - AMIS集成适配器
 * 用于将Cards SDK与现有的AMIS系统集成
 */
(function(global) {
    'use strict';

    /**
     * AMIS Cards适配器类
     */
    class AmisCardsAdapter {
        constructor(options = {}) {
            this.options = {
                amisScope: global.amisScope || global.amis,
                cardsSDK: null,
                ...options
            };
            
            this.init();
        }

        /**
         * 初始化适配器
         */
        init() {
            if (typeof CodeSpiritCards !== 'undefined') {
                this.cardsSDK = new CodeSpiritCards.SDK(this.options);
                this.registerAmisRenderer();
                console.log('AMIS Cards适配器初始化完成');
            } else {
                console.error('CodeSpirit Cards SDK未找到');
            }
        }

        /**
         * 注册AMIS渲染器
         */
        registerAmisRenderer() {
            // 检查 Amis 是否可用
            if (typeof global.amisRequire === 'undefined' && typeof global.amis === 'undefined') {
                console.warn('AMIS未找到，跳过渲染器注册');
                return;
            }

            // 使用 amisRequire 获取 Amis 组件（推荐方式）
            if (typeof global.amisRequire !== 'undefined') {
                try {
                    const React = global.amisRequire('react');
                    const amisCore = global.amisRequire('@fex/amis-core');
                    
                    if (amisCore && amisCore.Renderer && React) {
                        this.registerWithAmisCore(amisCore, React);
                        return;
                    }
                } catch (error) {
                    console.warn('amisRequire 方式注册失败，尝试备用方式:', error);
                }
            }

            // 备用方式：使用全局 amis 对象
            if (typeof global.amis !== 'undefined') {
                this.registerWithAmisGlobal(global.amis);
                return;
            }

            console.warn('无法找到合适的 Amis 注册方式');
        }

        /**
         * 使用 amisCore 注册渲染器（推荐方式）
         */
        registerWithAmisCore(amisCore, React) {

            // 注册统计卡片渲染器
            amisCore.Renderer({
                type: 'codespirit-cards',
                autoVar: true
            })(class extends React.Component {
                static displayName = 'CodeSpiritCards';

                constructor(props) {
                    super(props);
                    this.cardsContainer = null;
                    this.cardInstances = new Map();
                }

                componentDidMount() {
                    this.renderCards();
                }

                componentDidUpdate(prevProps) {
                    if (prevProps.cards !== this.props.cards) {
                        this.renderCards();
                    }
                }

                componentWillUnmount() {
                    this.destroyCards();
                }

                async renderCards() {
                    const { cards, data } = this.props;
                    
                    if (!cards || !Array.isArray(cards)) {
                        return;
                    }

                    // 处理卡片配置
                    const processedCards = this.processCardConfigs(cards, data);
                    
                    // 渲染到容器
                    if (this.cardsContainer && this.cardsSDK) {
                        try {
                            await this.cardsSDK.render(this.cardsContainer, processedCards);
                        } catch (error) {
                            console.error('渲染Cards失败:', error);
                        }
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

                        // 数据模板替换
                        if (processedCard.data && data) {
                            processedCard.data = this.replaceDataTemplates(processedCard.data, data);
                        }

                        // API路径处理
                        if (processedCard.dataSource) {
                            processedCard.dataSource = this.resolveApiPath(processedCard.dataSource, data);
                        }

                        return processedCard;
                    });
                }

                replaceDataTemplates(cardData, contextData) {
                    const result = { ...cardData };
                    
                    // 简单模板替换
                    Object.keys(result).forEach(key => {
                        if (typeof result[key] === 'string' && result[key].includes('${')) {
                            result[key] = result[key].replace(/\$\{(\w+)\}/g, (match, prop) => {
                                return contextData[prop] || match;
                            });
                        } else if (typeof result[key] === 'object' && result[key] !== null) {
                            result[key] = this.replaceDataTemplates(result[key], contextData);
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

                destroyCards() {
                    if (this.cardsSDK) {
                        this.cardInstances.forEach((card, id) => {
                            this.cardsSDK.destroy(id);
                        });
                        this.cardInstances.clear();
                    }
                }

                render() {
                    const { className, style } = this.props;
                    
                    return React.createElement('div', {
                        ref: (ref) => { this.cardsContainer = ref; },
                        className: `codespirit-cards-container ${className || ''}`,
                        style: style || {}
                    });
                }
            });

            // 注册单个卡片渲染器
            amisCore.Renderer({
                type: 'codespirit-stat-card'
            })(class extends React.Component {
                static displayName = 'CodeSpiritStatCard';

                constructor(props) {
                    super(props);
                    this.cardRef = null;
                }

                componentDidMount() {
                    this.renderStatCard();
                }

                componentDidUpdate(prevProps) {
                    if (JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data)) {
                        this.renderStatCard();
                    }
                }

                async renderStatCard() {
                    const { data, title, subtitle, theme, size } = this.props;
                    
                    if (!this.cardRef || !data) return;

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

                    try {
                        const renderer = new CodeSpiritCards.StatCardRenderer();
                        const cardElement = await renderer.render(cardConfig);
                        
                        // 清空容器并添加新卡片
                        this.cardRef.innerHTML = '';
                        this.cardRef.appendChild(cardElement);
                    } catch (error) {
                        console.error('渲染统计卡片失败:', error);
                    }
                }

                render() {
                    const { className, style } = this.props;
                    
                    return React.createElement('div', {
                        ref: (ref) => { this.cardRef = ref; },
                        className: `codespirit-stat-card-wrapper ${className || ''}`,
                        style: style || {}
                    });
                }
            });

            // 注册Amis Chart卡片渲染器（直接使用）
            amisCore.Renderer({
                type: 'codespirit-amis-chart'
            })(class extends React.Component {
                static displayName = 'CodeSpiritAmisChart';

                constructor(props) {
                    super(props);
                    this.chartRef = null;
                }

                componentDidMount() {
                    this.renderAmisChart();
                }

                componentDidUpdate(prevProps) {
                    if (JSON.stringify(prevProps.data) !== JSON.stringify(this.props.data) ||
                        prevProps.chartType !== this.props.chartType) {
                        this.renderAmisChart();
                    }
                }

                async renderAmisChart() {
                    const { data, title, subtitle, chartType, height, theme } = this.props;
                    
                    if (!this.chartRef || !data) return;

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

                    try {
                        const renderer = new CodeSpiritCards.AmisChartCardRenderer();
                        const cardElement = await renderer.render(cardConfig);
                        
                        // 清空容器并添加新卡片
                        this.chartRef.innerHTML = '';
                        this.chartRef.appendChild(cardElement);
                    } catch (error) {
                        console.error('渲染Amis图表卡片失败:', error);
                    }
                }

                render() {
                    const { className, style } = this.props;
                    
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
        registerWithAmisGlobal(amis) {
            if (!amis.Renderer) {
                console.warn('amis.Renderer 方法不存在，跳过渲染器注册');
                return;
            }

            try {
                // 尝试使用全局 amis 对象注册
                const React = window.React || amis.React;
                
                if (!React) {
                    console.warn('React 未找到，无法注册渲染器');
                    return;
                }

                // 注册统计卡片渲染器
                amis.Renderer({
                    type: 'codespirit-cards',
                    autoVar: true
                })(class extends React.Component {
                    static displayName = 'CodeSpiritCards';

                    constructor(props) {
                        super(props);
                        this.cardsContainer = null;
                        this.cardInstances = new Map();
                    }

                    render() {
                        const { className, style } = this.props;
                        
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

    // 全局暴露
    if (typeof window !== 'undefined') {
        window.AmisCardsAdapter = AmisCardsAdapter;
        window.AmisCardsUtils = AmisCardsUtils;
    }

    // 自动初始化（如果环境允许）
    if (typeof window !== 'undefined') {
        // 延迟初始化函数
        const initializeAdapter = () => {
            if (typeof CodeSpiritCards !== 'undefined' && 
                (typeof window.amisRequire !== 'undefined' || typeof window.amis !== 'undefined')) {
                try {
                    window.amisCardsAdapter = new AmisCardsAdapter();
                    console.log('AmisCardsAdapter 自动初始化成功');
                } catch (error) {
                    console.error('AmisCardsAdapter 自动初始化失败:', error);
                }
            } else {
                console.warn('AmisCardsAdapter 初始化条件不满足，将在稍后重试');
                // 如果条件不满足，延迟重试
                setTimeout(initializeAdapter, 1000);
            }
        };

        // 等待DOM加载完成后初始化
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeAdapter);
        } else {
            // DOM已经加载完成，延迟一段时间后初始化
            setTimeout(initializeAdapter, 500);
        }
    }

})(typeof window !== 'undefined' ? window : global); 