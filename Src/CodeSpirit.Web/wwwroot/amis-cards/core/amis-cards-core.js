/**
 * CodeSpirit Amis Cards V2.0 - 核心SDK
 * 基于 Amis Page 组件的统一表格卡片系统
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 * @description 提供配置化的卡片渲染能力，与现有 cards-sdk 完全隔离
 */

// 初始化全局命名空间
window.AmisCards = window.AmisCards || {};

/**
 * AmisCards 核心类
 * 负责管理渲染器、主题、数据服务等核心功能
 */
class AmisCardsCore {
    /**
     * 构造函数
     * @param {Object} options 初始化选项
     * @param {HTMLElement|string} options.container 容器元素或选择器
     * @param {string} options.theme 主题名称，默认为 'default'
     * @param {Object} options.config 全局配置
     */
    constructor(options = {}) {
        this.options = options;
        this.container = this.resolveContainer(options.container);
        this.renderers = new Map();
        this.theme = options.theme || 'default';
        this.config = options.config || {};
        this.amisInstance = null;
        
        // 初始化核心功能
        this.init();
    }

    /**
     * 初始化核心功能
     */
    init() {
        console.log('[AmisCards] 正在初始化核心SDK...');
        
        // 检查依赖
        this.checkDependencies();
        
        // 初始化默认渲染器
        this.registerDefaultRenderers();
        
        // 设置主题
        this.setTheme(this.theme);
        
        console.log('[AmisCards] 核心SDK初始化完成');
    }

    /**
     * 检查必要依赖
     */
    checkDependencies() {
        if (typeof window.amisRequire === 'undefined' && typeof window.amis === 'undefined') {
            throw new Error('[AmisCards] 缺少 Amis 依赖，请确保已正确加载 Amis SDK');
        }
        
        // 检查 TokenManager (现有认证系统)
        if (typeof window.TokenManager === 'undefined') {
            console.warn('[AmisCards] TokenManager 未找到，某些功能可能受限');
        }
    }

    /**
     * 解析容器元素
     * @param {HTMLElement|string} container 容器元素或选择器
     * @returns {HTMLElement} 容器元素
     */
    resolveContainer(container) {
        if (typeof container === 'string') {
            const element = document.querySelector(container);
            if (!element) {
                throw new Error(`[AmisCards] 无法找到容器元素: ${container}`);
            }
            return element;
        } else if (container instanceof HTMLElement) {
            return container;
        } else {
            throw new Error('[AmisCards] 无效的容器元素');
        }
    }

    /**
     * 注册渲染器
     * @param {string} type 渲染器类型
     * @param {Function} renderer 渲染器类
     */
    registerRenderer(type, renderer) {
        if (typeof renderer !== 'function') {
            throw new Error(`[AmisCards] 渲染器必须是一个类或函数: ${type}`);
        }
        
        this.renderers.set(type, renderer);
        console.log(`[AmisCards] 已注册渲染器: ${type}`);
    }

    /**
     * 注册默认渲染器
     */
    registerDefaultRenderers() {
        // 从全局注册表中导入已经注册的渲染器
        if (window.AmisCards.renderers && window.AmisCards.renderers.size > 0) {
            window.AmisCards.renderers.forEach((renderer, type) => {
                this.registerRenderer(type, renderer);
            });
        }
        console.log('[AmisCards] 等待注册默认渲染器...');
    }

    /**
     * 获取渲染器
     * @param {string} type 渲染器类型
     * @returns {Function} 渲染器类
     */
    getRenderer(type) {
        const renderer = this.renderers.get(type);
        if (!renderer) {
            throw new Error(`[AmisCards] 未找到渲染器: ${type}`);
        }
        return renderer;
    }

    /**
     * 渲染卡片
     * @param {Array|Object} cards 卡片配置数组或单个卡片配置
     * @returns {Promise} 渲染完成的Promise
     */
    async render(cards) {
        try {
            console.log('[AmisCards] 开始渲染卡片...', cards);
            
            // 标准化卡片配置
            const cardConfigs = Array.isArray(cards) ? cards : [cards];
            console.log('[AmisCards] 标准化后的卡片配置数量:', cardConfigs.length);
            
            // 保存当前卡片配置，用于主题切换时重新渲染
            this.currentCards = [...cardConfigs];
            
            // 验证卡片配置
            console.log('[AmisCards] 开始验证卡片配置...');
            this.validateCards(cardConfigs);
            console.log('[AmisCards] 卡片配置验证通过');
            
            // 生成 Amis 页面配置
            console.log('[AmisCards] 开始生成页面配置...');
            const pageConfig = this.generatePageConfig(cardConfigs);
            console.log('[AmisCards] 页面配置生成完成:', pageConfig);
            
            // 使用 Amis 渲染页面
            console.log('[AmisCards] 开始渲染页面...');
            await this.renderAmisPage(pageConfig);
            
            console.log('[AmisCards] 卡片渲染完成');
            
        } catch (error) {
            console.error('[AmisCards] 渲染失败:', error);
            console.error('[AmisCards] 错误堆栈:', error.stack);
            throw error;
        }
    }

    /**
     * 验证卡片配置
     * @param {Array} cards 卡片配置数组
     */
    validateCards(cards) {
        cards.forEach((card, index) => {
            if (!card.type) {
                throw new Error(`[AmisCards] 卡片配置缺少 type 属性 (index: ${index})`);
            }
            
            if (!this.renderers.has(card.type)) {
                throw new Error(`[AmisCards] 未支持的卡片类型: ${card.type} (index: ${index})`);
            }
            
            if (!card.id) {
                card.id = `amis-card-${Date.now()}-${index}`;
            }
        });
    }

    /**
     * 生成 Amis 页面配置
     * @param {Array} cards 卡片配置数组
     * @returns {Object} Amis 页面配置
     */
    generatePageConfig(cards) {
        const bodyItems = [];
        
        cards.forEach(card => {
            const RendererClass = this.getRenderer(card.type);
            const renderer = new RendererClass(card);
            const cardConfig = renderer.generateAmisConfig();
            
            bodyItems.push(cardConfig);
        });

        // 检测是否为混合卡片布局
        const isMixedLayout = this.detectMixedLayout(cards);
        
        // 如果配置中有自定义的 pageSchema，使用它
        if (this.config.pageSchema) {
            console.log('[AmisCards] 使用自定义 pageSchema');
            const customPageConfig = { ...this.config.pageSchema };
            
            // 将卡片内容添加到自定义页面配置的 body 中
            const cardsBody = isMixedLayout ? this.generateMixedLayout(bodyItems) : this.generateUniformLayout(bodyItems);
            
            if (customPageConfig.body) {
                // 如果自定义配置已有 body，将卡片内容追加到其中
                if (Array.isArray(customPageConfig.body)) {
                    customPageConfig.body.push(...cardsBody);
                } else {
                    customPageConfig.body = [customPageConfig.body, ...cardsBody];
                }
            } else {
                // 如果没有 body，直接设置
                customPageConfig.body = cardsBody;
            }
            
            // 确保主题类名被应用
            if (!customPageConfig.className || !customPageConfig.className.includes('amis-cards-theme-')) {
                customPageConfig.className = `${customPageConfig.className || ''} amis-cards-page amis-cards-theme-${this.theme}`.trim();
            }
            
            // 为页面配置添加初始数据（用于 title、subTitle、remark 中的模板变量）
            if (!customPageConfig.data) {
                customPageConfig.data = {};
            }
            
            // 添加当前时间，用于 lastUpdate 等动态显示
            customPageConfig.data.lastUpdate = new Date().toLocaleTimeString();
            customPageConfig.data.now = new Date();
            
            // 支持自动刷新配置
            if (this.config.autoRefresh) {
                // 添加刷新状态数据
                customPageConfig.data.refreshStatus = '准备就绪';
                customPageConfig.data.isRefreshing = false;
                customPageConfig.data.refreshCount = 0;
                
                // 配置定时刷新
                if (this.config.refreshInterval) {
                    customPageConfig.interval = this.config.refreshInterval;
                    customPageConfig.silentPolling = true;
                    customPageConfig.stopAutoRefreshWhen = '${autoRefreshEnabled === false}';
                    
                    // 添加刷新前后的事件处理
                    customPageConfig.onEvent = customPageConfig.onEvent || {};
                    customPageConfig.onEvent.fetchInited = customPageConfig.onEvent.fetchInited || {};
                    customPageConfig.onEvent.fetchInited.actions = customPageConfig.onEvent.fetchInited.actions || [];
                    customPageConfig.onEvent.fetchInited.actions.push({
                        actionType: 'setValue',
                        args: {
                            value: {
                                isRefreshing: false,
                                refreshStatus: '数据已更新',
                                lastUpdate: new Date().toLocaleTimeString(),
                                refreshCount: '${refreshCount + 1}'
                            }
                        }
                    });
                    
                    customPageConfig.onEvent.fetchSchemaInited = customPageConfig.onEvent.fetchSchemaInited || {};
                    customPageConfig.onEvent.fetchSchemaInited.actions = customPageConfig.onEvent.fetchSchemaInited.actions || [];
                    customPageConfig.onEvent.fetchSchemaInited.actions.push({
                        actionType: 'setValue',
                        args: {
                            value: {
                                isRefreshing: true,
                                refreshStatus: '正在刷新...'
                            }
                        }
                    });
                }
            }
            
            return customPageConfig;
        }
        
        // 生成默认的页面配置
        const defaultConfig = {
            type: 'page',
            title: this.config.pageTitle || 'AmisCards 仪表板',
            className: `amis-cards-page amis-cards-theme-${this.theme}`,
            body: isMixedLayout ? this.generateMixedLayout(bodyItems) : this.generateUniformLayout(bodyItems)
        };
        
        // 如果启用自动刷新，添加相关配置
        if (this.config.autoRefresh && this.config.refreshInterval) {
            defaultConfig.interval = this.config.refreshInterval;
            defaultConfig.silentPolling = true;
            defaultConfig.data = {
                refreshStatus: '准备就绪',
                isRefreshing: false,
                refreshCount: 0,
                lastUpdate: new Date().toLocaleTimeString()
            };
        }
        
        return defaultConfig;
    }

    /**
     * 从卡片配置中获取卡片类型
     * @param {Object} itemConfig 卡片配置
     * @returns {string} 卡片类型
     */
    getCardTypeFromConfig(itemConfig) {
        console.log('[AmisCards] 检测卡片类型，配置:', itemConfig);
        
        // 从className中提取类型，或者从其他属性推断
        if (itemConfig.className) {
            if (itemConfig.className.includes('amis-cards-stat')) {
                console.log('[AmisCards] 通过className检测到stat类型');
                return 'stat';
            }
            if (itemConfig.className.includes('amis-cards-chart')) {
                console.log('[AmisCards] 通过className检测到chart类型');
                return 'chart';
            }
            if (itemConfig.className.includes('amis-cards-table')) {
                console.log('[AmisCards] 通过className检测到table类型');
                return 'table';
            }
            if (itemConfig.className.includes('amis-cards-info')) {
                console.log('[AmisCards] 通过className检测到info类型');
                return 'info';
            }
        }
        
        // 从body内容推断类型
        if (itemConfig.body) {
            const firstBodyItem = Array.isArray(itemConfig.body) ? itemConfig.body[0] : itemConfig.body;
            if (firstBodyItem) {
                if (firstBodyItem.type === 'table') {
                    console.log('[AmisCards] 通过body内容检测到table类型');
                    return 'table';
                }
                if (firstBodyItem.type === 'chart') {
                    console.log('[AmisCards] 通过body内容检测到chart类型');
                    return 'chart';
                }
                if (firstBodyItem.className && firstBodyItem.className.includes('stat')) {
                    console.log('[AmisCards] 通过body内容检测到stat类型');
                    return 'stat';
                }
            }
        }
        
        console.log('[AmisCards] 使用默认类型');
        return 'default';
    }

    /**
     * 检测是否为混合布局
     * @param {Array} cards 卡片配置数组
     * @returns {boolean} 是否为混合布局
     */
    detectMixedLayout(cards) {
        if (cards.length < 2) return false;
        
        const types = new Set();
        cards.forEach(card => {
            types.add(card.type);
        });
        
        // 如果有2种或以上不同类型的卡片，认为是混合布局
        const isMixed = types.size > 1;
        console.log('[AmisCards] 检测布局类型:', isMixed ? '混合布局' : '统一布局', '卡片类型:', Array.from(types));
        return isMixed;
    }

    /**
     * 生成混合布局
     * @param {Array} bodyItems 卡片项目
     * @returns {Array} 布局配置
     */
    generateMixedLayout(bodyItems) {
        console.log('[AmisCards] 生成混合布局');
        
        // 将卡片按类型分组
        const statCards = [];
        const chartCards = [];
        const tableCards = [];
        const infoCards = [];
        const otherCards = [];
        
        bodyItems.forEach((item, index) => {
            const cardType = this.getCardTypeFromConfig(item);
            switch (cardType) {
                case 'stat':
                    statCards.push(item);
                    break;
                case 'chart':
                    chartCards.push(item);
                    break;
                case 'table':
                    tableCards.push(item);
                    break;
                case 'info':
                    infoCards.push(item);
                    break;
                default:
                    otherCards.push(item);
                    break;
            }
        });
        
        const layout = [];
        
        // 第一行：统计卡片（如果有的话）
        if (statCards.length > 0) {
            layout.push({
                type: 'grid',
                className: 'amis-cards-stats-row',
                columns: statCards.map(card => ({
                    body: [card],
                    md: Math.max(1, Math.floor(12 / Math.min(statCards.length, 4))), // 最多4列
                    className: 'amis-cards-stat-item'
                })),
                gap: 'md'
            });
        }
        
        // 第二行：图表和信息卡片混合
        if (chartCards.length > 0 || infoCards.length > 0) {
            const mixedItems = [...chartCards, ...infoCards];
            layout.push({
                type: 'grid',
                className: 'amis-cards-mixed-row',
                columns: mixedItems.map(card => {
                    const cardType = this.getCardTypeFromConfig(card);
                    return {
                        body: [card],
                        md: cardType === 'chart' ? 8 : 4, // 图表占2/3，信息占1/3
                        className: `amis-cards-${cardType}-item`
                    };
                }),
                gap: 'md'
            });
        }
        
        // 第三行：表格卡片（如果有的话）
        if (tableCards.length > 0) {
            tableCards.forEach(card => {
                layout.push({
                    type: 'grid',
                    className: 'amis-cards-table-row',
                    columns: [{
                        body: [card],
                        md: 12, // 表格占满整行
                        className: 'amis-cards-table-item'
                    }],
                    gap: 'md'
                });
            });
        }
        
        // 其他卡片
        if (otherCards.length > 0) {
            layout.push({
                type: 'grid',
                className: 'amis-cards-other-row',
                columns: otherCards.map(card => ({
                    body: [card],
                    md: 6,
                    className: 'amis-cards-default-item'
                })),
                gap: 'md'
            });
        }
        
        return layout;
    }

    /**
     * 生成统一布局
     * @param {Array} bodyItems 卡片项目
     * @returns {Array} 布局配置
     */
    generateUniformLayout(bodyItems) {
        console.log('[AmisCards] 生成统一布局');
        
        return [
            {
                type: 'grid',
                className: 'amis-cards-grid',
                columns: bodyItems.map(item => ({
                    body: [item],
                    className: 'amis-cards-grid-item'
                })),
                gap: 'lg'
            }
        ];
    }

    /**
     * 使用 Amis 渲染页面
     * @param {Object} pageConfig Amis 页面配置
     */
    async renderAmisPage(pageConfig) {
        console.log('[AmisCards] 开始渲染Amis页面:', pageConfig);
        
        // 清空容器
        this.container.innerHTML = '';
        
        try {
            // 使用 Amis 渲染
            if (window.amisRequire) {
                // 使用amisRequire获取embed函数，但不重新分配window.amis
                console.log('[AmisCards] 使用 amisRequire 获取 amis/embed');
                const amisEmbed = window.amisRequire('amis/embed');
                this.amisInstance = amisEmbed.embed(this.container, pageConfig, {
                    data: {
                        now: new Date()
                    },
                    requestAdaptor: (api) => {
                        // 检查TokenManager是否存在
                        const token = window.TokenManager ? window.TokenManager.getToken() : null;
                        return {
                            ...api,
                            headers: {
                                ...api.headers,
                                'Authorization': token ? 'Bearer ' + token : '',
                                'X-Forwarded-With': 'CodeSpirit'
                            }
                        };
                    }
                }, {
                    theme: 'antd',
                    locale: 'zh-CN'
                });
            } else if (window.amis && window.amis.embed) {
                // 新版本 Amis (6.x)
                console.log('[AmisCards] 使用 amis.embed 渲染');
                this.amisInstance = window.amis.embed(this.container, pageConfig, {
                    data: {
                        now: new Date()
                    },
                    requestAdaptor: (api) => {
                        const token = window.TokenManager ? window.TokenManager.getToken() : null;
                        return {
                            ...api,
                            headers: {
                                ...api.headers,
                                'Authorization': token ? 'Bearer ' + token : '',
                                'X-Forwarded-With': 'CodeSpirit'
                            }
                        };
                    }
                }, {
                    theme: 'antd',
                    locale: 'zh-CN'
                });
            } else if (window.amis && window.amis.render) {
                // 备用渲染方法
                console.log('[AmisCards] 使用 amis.render 渲染');
                this.amisInstance = window.amis.render(pageConfig, this.container);
            } else {
                throw new Error('[AmisCards] 无法找到有效的 Amis 渲染方法');
            }
            
            console.log('[AmisCards] Amis页面渲染完成');
            
        } catch (error) {
            console.error('[AmisCards] Amis渲染失败:', error);
            
            // 显示备用内容
            this.container.innerHTML = `
                <div style="text-align: center; padding: 2rem; border: 2px dashed #ddd; border-radius: 8px; color: #666;">
                    <i class="fa fa-exclamation-triangle" style="font-size: 2rem; margin-bottom: 1rem; color: #f39c12;"></i>
                    <div style="font-size: 1.1rem; font-weight: 600; margin-bottom: 0.5rem;">Amis渲染失败</div>
                    <div style="font-size: 0.9rem; margin-bottom: 1rem;">${error.message}</div>
                    <div style="font-size: 0.8rem; color: #999;">请检查Amis库是否正确加载</div>
                </div>
            `;
            
            throw error;
        }
    }

    /**
     * 更新工具栏按钮状态
     * @param {string} buttonClass 按钮的类名
     * @param {boolean} active 是否激活
     */
    updateToolbarButton(buttonClass, active = true) {
        if (this.container) {
            const buttons = this.container.querySelectorAll(`.${buttonClass}`);
            buttons.forEach(button => {
                if (active) {
                    button.classList.add('active');
                } else {
                    button.classList.remove('active');
                }
            });
        }
    }

    /**
     * 获取实例状态信息
     * @returns {Object} 状态信息
     */
    getState() {
        return {
            theme: this.theme,
            cardsCount: this.currentCards ? this.currentCards.length : 0,
            renderersCount: this.renderers.size,
            hasAmisInstance: !!this.amisInstance
        };
    }

    /**
     * 设置主题
     * @param {string} theme 主题名称
     * @param {boolean} rerender 是否重新渲染卡片
     */
    async setTheme(theme, rerender = true) {
        const oldTheme = this.theme;
        this.theme = theme;
        
        console.log(`[AmisCards] 切换主题: ${oldTheme} -> ${theme}`);
        
        // 应用主题样式
        if (window.AmisCards.ThemeUtils) {
            window.AmisCards.ThemeUtils.applyTheme(theme, this.container);
        }
        
        // 更新容器的主题类名
        if (this.container) {
            // 移除旧的主题类名
            const classList = Array.from(this.container.classList);
            classList.forEach(className => {
                if (className.startsWith('amis-cards-theme-')) {
                    this.container.classList.remove(className);
                }
            });
            
            // 添加新的主题类名
            this.container.classList.add(`amis-cards-theme-${theme}`);
            
            // 添加过渡效果
            this.container.style.transition = 'background-color 0.3s ease, color 0.3s ease';
            setTimeout(() => {
                this.container.style.transition = '';
            }, 300);
        }
        
        // 触发主题切换事件（用于深色主题增强器）
        const themeEvent = new CustomEvent('themeChanged', {
            detail: { 
                theme: theme,
                oldTheme: oldTheme,
                container: this.container
            }
        });
        document.dispatchEvent(themeEvent);
        
        // 如果有当前卡片配置且需要重新渲染，则重新渲染
        if (rerender && this.currentCards && this.currentCards.length > 0) {
            console.log('[AmisCards] 重新渲染卡片以应用新主题');
            try {
                await this.render(this.currentCards);
                console.log('[AmisCards] 主题切换并重新渲染完成');
            } catch (error) {
                console.error('[AmisCards] 主题切换时重新渲染失败:', error);
                // 恢复原主题
                this.theme = oldTheme;
                throw error;
            }
        } else {
            console.log(`[AmisCards] 主题已设置为: ${theme}`);
        }
    }

    /**
     * 销毁实例
     */
    destroy() {
        if (this.amisInstance && typeof this.amisInstance.destroy === 'function') {
            this.amisInstance.destroy();
        }
        
        if (this.container) {
            this.container.innerHTML = '';
        }
        
        this.renderers.clear();
        console.log('[AmisCards] 实例已销毁');
    }

    /**
     * 获取版本信息
     * @returns {string} 版本号
     */
    static getVersion() {
        return '2.0.0';
    }
}

// 导出核心类
window.AmisCards.Core = AmisCardsCore;

// 创建便捷的全局方法
window.AmisCards.create = function(options) {
    return new AmisCardsCore(options);
};

// 版本信息
window.AmisCards.version = AmisCardsCore.getVersion();

console.log(`[AmisCards] 核心SDK已加载 (v${AmisCardsCore.getVersion()})`); 