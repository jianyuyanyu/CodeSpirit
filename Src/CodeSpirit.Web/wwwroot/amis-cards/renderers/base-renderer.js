/**
 * CodeSpirit Amis Cards V2.0 - 基础渲染器
 * 所有卡片渲染器的基类，提供通用功能
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 基础渲染器类
 * 提供所有卡片渲染器的通用功能和接口
 */
class BaseRenderer {
    /**
     * 构造函数
     * @param {Object} config 卡片配置
     */
    constructor(config) {
        // 基础配置
        this.config = this.normalizeConfig(config);
        this.id = this.config.id || window.AmisCards.Utils.generateId('card');
        this.type = this.config.type;
        
        // 样式配置
        this.size = this.config.size || 'medium';
        this.theme = this.config.theme || this.config.style?.theme || 'default';
        this.className = this.config.className || '';
        
        // 数据配置
        this.dataConfig = this.config.data || {};
        this.autoRefresh = this.config.autoRefresh || false;
        this.refreshInterval = this.config.refreshInterval || 30000;
        
        // 权限配置
        this.permissions = this.config.permissions || [];
        
        // 内部状态
        this.isRendered = false;
        this.isLoading = false;
        this.hasError = false;
        this.refreshTimer = null;
        this.data = null;
        
        console.log(`[AmisCards.BaseRenderer] 创建渲染器: ${this.type} (${this.id})`);
    }

    /**
     * 标准化配置
     * @param {Object} config 原始配置
     * @returns {Object} 标准化后的配置
     */
    normalizeConfig(config) {
        // 深拷贝配置，避免修改原始配置
        const normalized = window.AmisCards.Utils.deepClone(config);
        
        // 设置默认值
        if (!normalized.style) normalized.style = {};
        if (!normalized.data) normalized.data = {};
        
        return normalized;
    }

    /**
     * 生成 Amis 配置
     * 这是核心方法，子类必须实现
     * @returns {Object} Amis 组件配置
     */
    generateAmisConfig() {
        throw new Error(`[AmisCards.BaseRenderer] generateAmisConfig 方法必须在子类中实现 (${this.type})`);
    }

    /**
     * 获取基础 Amis 配置
     * 提供所有卡片共同的基础配置
     * @returns {Object} 基础 Amis 配置
     */
    getBaseAmisConfig() {
        return {
            type: 'panel',
            id: this.id,
            className: this.buildClassName(),
            title: this.config.title,
            subTitle: this.config.subtitle,
            headerToolbar: this.buildHeaderToolbar(),
            body: this.getCardBody(),
            actions: this.buildActions()
        };
    }

    /**
     * 构建CSS类名
     * @returns {string} 完整的CSS类名
     */
    buildClassName() {
        const classes = [
            'amis-cards-card',
            `amis-cards-card-${this.type}`,
            `amis-cards-size-${this.size}`,
            `amis-cards-theme-${this.theme}`
        ];
        
        if (this.className) {
            classes.push(this.className);
        }
        
        if (this.isLoading) {
            classes.push('amis-cards-loading');
        }
        
        if (this.hasError) {
            classes.push('amis-cards-error');
        }
        
        return classes.join(' ');
    }

    /**
     * 构建头部工具栏
     * @returns {Array} 工具栏配置
     */
    buildHeaderToolbar() {
        const toolbar = [];
        
        // 刷新按钮
        if (this.autoRefresh || this.config.showRefreshButton !== false) {
            toolbar.push({
                type: 'button',
                icon: 'fa fa-refresh',
                level: 'link',
                size: 'sm',
                tooltip: '刷新数据',
                actionType: 'ajax',
                api: {
                    method: 'post',
                    url: 'javascript:void(0)',
                    data: {},
                    onSuccess: () => this.refresh()
                }
            });
        }
        
        // 全屏按钮
        if (this.config.showFullscreenButton) {
            toolbar.push({
                type: 'button',
                icon: 'fa fa-expand',
                level: 'link',
                size: 'sm',
                tooltip: '全屏显示',
                actionType: 'dialog',
                dialog: {
                    title: this.config.title,
                    size: 'full',
                    body: this.getCardBody()
                }
            });
        }
        
        // 设置按钮
        if (this.config.showSettingsButton) {
            toolbar.push({
                type: 'button',
                icon: 'fa fa-cog',
                level: 'link',
                size: 'sm',
                tooltip: '设置',
                actionType: 'drawer',
                drawer: {
                    title: '卡片设置',
                    size: 'md',
                    body: this.buildSettingsForm()
                }
            });
        }
        
        return toolbar;
    }

    /**
     * 获取卡片主体内容
     * 子类需要重写此方法
     * @returns {Array|Object} Amis 组件配置
     */
    getCardBody() {
        return [
            {
                type: 'alert',
                level: 'info',
                body: `${this.type} 渲染器的 getCardBody 方法需要在子类中实现`
            }
        ];
    }

    /**
     * 构建操作按钮
     * @returns {Array} 操作按钮配置
     */
    buildActions() {
        const actions = [];
        
        // 从配置中获取自定义操作
        if (this.config.actions && Array.isArray(this.config.actions)) {
            actions.push(...this.config.actions);
        }
        
        return actions;
    }

    /**
     * 构建设置表单
     * @returns {Object} 设置表单配置
     */
    buildSettingsForm() {
        return {
            type: 'form',
            body: [
                {
                    type: 'input-text',
                    name: 'title',
                    label: '卡片标题',
                    value: this.config.title
                },
                {
                    type: 'input-text',
                    name: 'subtitle',
                    label: '副标题',
                    value: this.config.subtitle
                },
                {
                    type: 'select',
                    name: 'size',
                    label: '卡片大小',
                    value: this.size,
                    options: [
                        { label: '小', value: 'small' },
                        { label: '中', value: 'medium' },
                        { label: '大', value: 'large' }
                    ]
                },
                {
                    type: 'select',
                    name: 'theme',
                    label: '主题',
                    value: this.theme,
                    options: [
                        { label: '默认', value: 'default' },
                        { label: '主要', value: 'primary' },
                        { label: '成功', value: 'success' },
                        { label: '警告', value: 'warning' },
                        { label: '危险', value: 'danger' },
                        { label: '信息', value: 'info' }
                    ]
                },
                {
                    type: 'switch',
                    name: 'autoRefresh',
                    label: '自动刷新',
                    value: this.autoRefresh
                },
                {
                    type: 'input-number',
                    name: 'refreshInterval',
                    label: '刷新间隔(秒)',
                    value: this.refreshInterval / 1000,
                    min: 5,
                    visibleOn: 'this.autoRefresh'
                }
            ],
            actions: [
                {
                    type: 'submit',
                    label: '保存',
                    level: 'primary',
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: 'custom',
                                    script: (context) => {
                                        this.updateConfig(context.event.data);
                                    }
                                }
                            ]
                        }
                    }
                }
            ]
        };
    }

    /**
     * 更新配置
     * @param {Object} newConfig 新配置
     */
    updateConfig(newConfig) {
        // 更新配置
        Object.assign(this.config, newConfig);
        
        // 更新内部状态
        this.size = newConfig.size || this.size;
        this.theme = newConfig.theme || this.theme;
        this.autoRefresh = newConfig.autoRefresh !== undefined ? newConfig.autoRefresh : this.autoRefresh;
        this.refreshInterval = (newConfig.refreshInterval || this.refreshInterval / 1000) * 1000;
        
        // 重新渲染
        this.refresh();
        
        console.log(`[AmisCards.BaseRenderer] 已更新配置: ${this.id}`);
    }

    /**
     * 加载数据
     * @returns {Promise} 数据加载Promise
     */
    async loadData() {
        if (!this.dataConfig.api && !this.dataConfig.url) {
            // 没有数据源，使用静态数据
            this.data = this.dataConfig.data || this.dataConfig;
            return this.data;
        }
        
        try {
            this.setLoading(true);
            
            const api = this.dataConfig.api || this.dataConfig.url;
            const params = this.dataConfig.params || {};
            const options = this.dataConfig.options || {};
            
            console.log(`[AmisCards.BaseRenderer] 开始加载数据: ${this.id}`);
            
            // 使用数据服务加载数据
            this.data = await window.AmisCards.dataService.get(api, params, options);
            
            console.log(`[AmisCards.BaseRenderer] 数据加载完成: ${this.id}`);
            
            return this.data;
            
        } catch (error) {
            console.error(`[AmisCards.BaseRenderer] 数据加载失败: ${this.id}`, error);
            this.setError(error.message);
            throw error;
        } finally {
            this.setLoading(false);
        }
    }

    /**
     * 设置加载状态
     * @param {boolean} loading 是否加载中
     */
    setLoading(loading) {
        this.isLoading = loading;
        if (loading) {
            this.hasError = false;
        }
    }

    /**
     * 设置错误状态
     * @param {string} errorMessage 错误信息
     */
    setError(errorMessage) {
        this.hasError = true;
        this.errorMessage = errorMessage;
        this.isLoading = false;
    }

    /**
     * 清除错误状态
     */
    clearError() {
        this.hasError = false;
        this.errorMessage = null;
    }

    /**
     * 刷新数据
     * @returns {Promise} 刷新Promise
     */
    async refresh() {
        try {
            this.clearError();
            await this.loadData();
            
            // 触发刷新事件
            this.emit('refresh', this.data);
            
        } catch (error) {
            this.emit('error', error);
        }
    }

    /**
     * 开始自动刷新
     */
    startAutoRefresh() {
        if (!this.autoRefresh) return;
        
        this.stopAutoRefresh();
        
        this.refreshTimer = setInterval(() => {
            this.refresh();
        }, this.refreshInterval);
        
        console.log(`[AmisCards.BaseRenderer] 已开始自动刷新: ${this.id} (间隔: ${this.refreshInterval}ms)`);
    }

    /**
     * 停止自动刷新
     */
    stopAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
            this.refreshTimer = null;
            console.log(`[AmisCards.BaseRenderer] 已停止自动刷新: ${this.id}`);
        }
    }

    /**
     * 检查权限
     * @param {string} permission 权限名称
     * @returns {boolean} 是否有权限
     */
    hasPermission(permission) {
        if (!this.permissions.length) return true;
        
        // 简单权限检查，可以根据实际需求扩展
        return this.permissions.includes(permission) || this.permissions.includes('*');
    }

    /**
     * 验证配置
     * @returns {boolean} 配置是否有效
     */
    validateConfig() {
        if (!this.config.type) {
            console.error(`[AmisCards.BaseRenderer] 缺少 type 配置: ${this.id}`);
            return false;
        }
        
        return true;
    }

    /**
     * 获取渲染器状态
     * @returns {Object} 状态信息
     */
    getState() {
        return {
            id: this.id,
            type: this.type,
            isRendered: this.isRendered,
            isLoading: this.isLoading,
            hasError: this.hasError,
            errorMessage: this.errorMessage,
            autoRefresh: this.autoRefresh,
            refreshInterval: this.refreshInterval
        };
    }

    /**
     * 触发事件
     * @param {string} eventName 事件名称
     * @param {*} data 事件数据
     */
    emit(eventName, data) {
        const event = new CustomEvent(`amis-cards-${eventName}`, {
            detail: {
                renderer: this,
                data: data,
                timestamp: Date.now()
            },
            bubbles: true
        });
        
        document.dispatchEvent(event);
    }

    /**
     * 销毁渲染器
     */
    destroy() {
        this.stopAutoRefresh();
        this.isRendered = false;
        
        console.log(`[AmisCards.BaseRenderer] 渲染器已销毁: ${this.id}`);
    }

    /**
     * 获取调试信息
     * @returns {Object} 调试信息
     */
    getDebugInfo() {
        return {
            id: this.id,
            type: this.type,
            config: this.config,
            state: this.getState(),
            data: this.data
        };
    }
}

// 导出基础渲染器类
window.AmisCards.BaseRenderer = BaseRenderer;

console.log('[AmisCards.BaseRenderer] 基础渲染器已加载'); 