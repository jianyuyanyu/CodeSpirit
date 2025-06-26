/**
 * CodeSpirit Amis Cards V2.0 - 统计卡片渲染器
 * 用于展示数值统计信息，支持趋势显示和实时更新
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 统计卡片渲染器类
 * 继承自 BaseRenderer，专门用于展示统计数据
 */
class StatRenderer extends window.AmisCards.BaseRenderer {
    /**
     * 构造函数
     * @param {Object} config 卡片配置
     */
    constructor(config) {
        super(config);

        // 统计卡片特有配置
        this.value = this.dataConfig.value || 0;
        this.label = this.dataConfig.label || '';
        this.unit = this.dataConfig.unit || '';
        this.prefix = this.dataConfig.prefix || '';
        this.suffix = this.dataConfig.suffix || '';
        this.trend = this.dataConfig.trend || null;
        this.formatter = this.dataConfig.formatter || null;
        this.target = this.dataConfig.target || null;
        this.showProgress = this.dataConfig.showProgress || false;

        // 图标配置
        this.icon = this.dataConfig.icon || null;
        this.iconColor = this.dataConfig.iconColor || null;
        this.iconSize = this.dataConfig.iconSize || 'md';
        this.iconPosition = this.dataConfig.iconPosition || 'left';
        this.iconBackground = this.dataConfig.iconBackground || null;
        this.iconBorder = this.dataConfig.iconBorder || false;

        // 动画配置
        this.animateValue = this.dataConfig.animateValue !== false;
        this.animationDuration = this.dataConfig.animationDuration || 2000;

        console.log(`[AmisCards.StatRenderer] 创建统计卡片: ${this.id}`);
    }

    /**
     * 生成 Amis 配置
     * @returns {Object} Amis 组件配置
     */
    generateAmisConfig() {
        const baseConfig = this.getBaseAmisConfig();

        return {
            ...baseConfig,
            body: this.getCardBody(),
            className: `${baseConfig.className} amis-cards-stat`
        };
    }

    /**
     * 获取卡片主体内容
     * @returns {Array} Amis 组件配置
     */
    getCardBody() {
        const body = [];

        // 主要统计区域
        body.push(this.buildMainStats());

        // 趋势信息
        if (this.trend) {
            body.push(this.buildTrend());
        }

        // 进度条
        if (this.showProgress && this.target) {
            body.push(this.buildProgress());
        }

        // 附加信息
        if (this.dataConfig.description) {
            body.push(this.buildDescription());
        }

        return body;
    }

    /**
     * 构建主要统计信息
     * @returns {Object} 主统计区域配置
     */
    buildMainStats() {

        let value = this.value;
        let displayValue = this.value;

        if (typeof(value) !== 'string') {
            value = this.formatValue(this.value);
            displayValue = this.buildDisplayValue(value);
        }
        // 构建值模板
        const valueTemplate = `<div class="stat-value-container">
            <span class="stat-prefix">${this.prefix}</span>
            <span class="stat-value" data-value="${this.value}">${displayValue}</span>
            <span class="stat-unit">${this.unit}</span>
            <span class="stat-suffix">${this.suffix}</span>
        </div>`;

        // 构建标签模板
        const labelTemplate = `<span class="stat-label">${this.label}</span>`;

        // 构建图标组件
        const iconComponent = this.buildIconComponent();

        // 根据图标位置决定布局
        const mainBody = [];

        if (this.iconPosition === 'top' && iconComponent) {
            // 图标在上方
            mainBody.push(iconComponent);
            mainBody.push({
                type: 'container',
                className: 'stat-content-container',
                body: [
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-value',
                        tpl: valueTemplate
                    },
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-label',
                        tpl: labelTemplate,
                        visibleOn: '!!this.label'
                    }
                ]
            });
        } else if (this.iconPosition === 'bottom' && iconComponent) {
            // 图标在下方
            mainBody.push({
                type: 'container',
                className: 'stat-content-container',
                body: [
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-value',
                        tpl: valueTemplate
                    },
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-label',
                        tpl: labelTemplate,
                        visibleOn: '!!this.label'
                    }
                ]
            });
            mainBody.push(iconComponent);
        } else {
            // 左右布局或无图标
            const contentContainer = {
                type: 'container',
                className: 'stat-content-container',
                body: [
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-value',
                        tpl: valueTemplate
                    },
                    {
                        type: 'tpl',
                        className: 'amis-cards-stat-label',
                        tpl: labelTemplate,
                        visibleOn: '!!this.label'
                    }
                ]
            };

            if (this.iconPosition === 'right' && iconComponent) {
                // 图标在右侧
                mainBody.push({
                    type: 'container',
                    className: 'stat-horizontal-container',
                    body: [
                        contentContainer,
                        iconComponent
                    ]
                });
            } else if (iconComponent) {
                // 图标在左侧（默认）
                mainBody.push({
                    type: 'container',
                    className: 'stat-horizontal-container',
                    body: [
                        iconComponent,
                        contentContainer
                    ]
                });
            } else {
                // 无图标
                mainBody.push(contentContainer);
            }
        }

        return {
            type: 'container',
            className: `amis-cards-stat-main stat-icon-${this.iconPosition}`,
            body: mainBody
        };
    }

    /**
     * 构建图标组件
     * @returns {Object|null} 图标组件配置
     */
    buildIconComponent() {
        if (!this.icon) {
            return null;
        }

        // 获取图标样式
        const iconStyles = this.getIconStyles();
        const iconClasses = this.getIconClasses();

        // 构建图标HTML
        let iconHtml = '';

        if (this.isUrlIcon(this.icon)) {
            // URL图标
            iconHtml = `<img src="${this.icon}" class="stat-icon-image" alt="icon" />`;
        } else {
            // FontAwesome图标
            const iconClass = this.normalizeIconClass(this.icon);
            iconHtml = `<i class="${iconClass}"></i>`;
        }

        // 构建完整的图标容器
        const iconTemplate = `
            <div class="stat-icon-container ${iconClasses}" style="${iconStyles}">
                ${iconHtml}
            </div>
        `;

        return {
            type: 'tpl',
            className: 'amis-cards-stat-icon',
            tpl: iconTemplate
        };
    }

    /**
     * 获取图标样式
     * @returns {string} CSS样式字符串
     */
    getIconStyles() {
        const styles = [];

        // 图标颜色
        if (this.iconColor) {
            styles.push(`color: ${this.iconColor}`);
        }

        // 图标背景
        if (this.iconBackground) {
            styles.push(`background-color: ${this.iconBackground}`);
        }

        // 图标边框
        if (this.iconBorder) {
            const borderColor = this.iconColor || 'currentColor';
            styles.push(`border: 1px solid ${borderColor}`);
        }

        return styles.join('; ');
    }

    /**
     * 获取图标CSS类
     * @returns {string} CSS类名字符串
     */
    getIconClasses() {
        const classes = [];

        // 图标尺寸
        classes.push(`stat-icon-${this.iconSize}`);

        // 图标位置
        classes.push(`stat-icon-pos-${this.iconPosition}`);

        // 图标背景样式
        if (this.iconBackground) {
            classes.push('stat-icon-with-bg');
        }

        // 图标边框样式
        if (this.iconBorder) {
            classes.push('stat-icon-with-border');
        }

        return classes.join(' ');
    }

    /**
     * 判断是否为URL图标
     * @param {string} icon 图标字符串
     * @returns {boolean} 是否为URL
     */
    isUrlIcon(icon) {
        return icon && (icon.startsWith('http') || icon.startsWith('//') || icon.startsWith('data:'));
    }

    /**
     * 标准化图标类名
     * @param {string} icon 图标字符串
     * @returns {string} 标准化的图标类名
     */
    normalizeIconClass(icon) {
        if (!icon) return '';

        // 如果已经包含fa类，直接返回
        if (icon.includes('fa ') || icon.startsWith('fa-')) {
            return icon.startsWith('fa-') ? `fa ${icon}` : icon;
        }

        // 如果是简单的图标名，添加fa前缀
        if (icon && !icon.includes(' ')) {
            return `fa fa-${icon}`;
        }

        return icon;
    }

    /**
     * 构建显示值
     * @param {string|number} value 格式化后的值
     * @returns {string} 显示值HTML
     */
    buildDisplayValue(value) {
        if (this.animateValue) {
            return `<span class="animated-value" data-target="${this.value}" data-duration="${this.animationDuration}">0</span>`;
        }
        return value;
    }

    /**
     * 构建趋势信息
     * @returns {Object} 趋势区域配置
     */
    buildTrend() {
        const { direction, value, period, percentage } = this.trend;
        const trendClass = `trend-${direction}`;
        const trendIcon = this.getTrendIcon(direction);
        const trendColor = this.getTrendColor(direction);

        // 构建趋势模板
        const periodHtml = period ? `<span class="trend-period">${period}</span>` : '';
        const trendTemplate = `<div class="trend-container ${trendClass}">
            <span class="trend-icon" style="color: ${trendColor}">
                <i class="fa ${trendIcon}"></i>
            </span>
            <span class="trend-value" style="color: ${trendColor}">
                ${this.formatTrendValue(value, percentage)}
            </span>
            ${periodHtml}
        </div>`;

        return {
            type: 'container',
            className: 'amis-cards-stat-trend',
            body: [
                {
                    type: 'tpl',
                    tpl: trendTemplate
                }
            ]
        };
    }

    /**
     * 构建进度条
     * @returns {Object} 进度条配置
     */
    buildProgress() {
        // 检查是否使用了 Amis 数据表达式
        const isAmisExpression = this.isAmisExpression(this.value) || this.isAmisExpression(this.target);
        
        if (isAmisExpression) {
            // 如果使用了 Amis 数据表达式，让 Amis 在运行时计算进度
            const progressColor = '#1890ff'; // 默认颜色
            const targetLabel = this.isAmisExpression(this.target) ? 
                `目标: ${this.target}` : 
                `目标: ${this.formatValue(this.target)}`;

            // 简化表达式构建，避免复杂的字符串拼接
            let progressValue;
            
            if (this.isAmisExpression(this.value) && this.isAmisExpression(this.target)) {
                // 都是表达式
                const valueExpr = this.extractExpression(this.value);
                const targetExpr = this.extractExpression(this.target);
                progressValue = `\${${targetExpr}>0?ROUND(${valueExpr} / ${targetExpr} * 100):0}`;
            } else if (this.isAmisExpression(this.value)) {
                // 只有值是表达式
                const valueExpr = this.extractExpression(this.value);
                progressValue = `\${${this.target}>0?ROUND(${valueExpr} / ${this.target} * 100):0}`;
            } else {
                // 只有目标是表达式
                const targetExpr = this.extractExpression(this.target);
                progressValue = `\${${targetExpr}>0?ROUND(${this.value} / ${targetExpr} * 100):0}`;
            }
            return {
                type: 'container',
                className: 'amis-cards-stat-progress',
                body: [
                    {
                        type: 'progress',
                        value: progressValue,
                        strokeWidth: 8,
                        className: 'stat-progress',
                        mode: 'line',
                        animate: true,
                        color: progressColor
                    },
                    {
                        type: 'tpl',
                        className: 'progress-label',
                        tpl: targetLabel
                    }
                ]
            };
        } else {
            // 静态值的处理逻辑
            const percentage = this.calculateProgress();
            // 格式化百分比显示，最多保留1位小数
            const formattedPercentage = percentage % 1 === 0 ?
                Math.round(percentage) :
                Math.round(percentage * 10) / 10;
            
            const progressColor = this.getProgressColor(percentage);
            const targetLabel = `目标: ${this.formatValue(this.target)}`;

            return {
                type: 'container',
                className: 'amis-cards-stat-progress',
                body: [
                    {
                        type: 'progress',
                        value: percentage,
                        strokeWidth: 8,
                        showText: true,
                        textTemplate: `${formattedPercentage}%`,
                        className: 'stat-progress',
                        mode: 'line',
                        animate: true,
                        color: progressColor
                    },
                    {
                        type: 'tpl',
                        className: 'progress-label',
                        tpl: targetLabel
                    }
                ]
            };
        }
    }

    /**
     * 判断是否为 Amis 数据表达式
     * @param {*} value 要检查的值
     * @returns {boolean} 是否为 Amis 表达式
     */
    isAmisExpression(value) {
        return typeof value === 'string' && value.startsWith('${') && value.endsWith('}');
    }

    /**
     * 安全地提取 Amis 表达式的内容
     * @param {string} expression Amis 表达式字符串
     * @returns {string} 提取的表达式内容
     */
    extractExpression(expression) {
        if (!this.isAmisExpression(expression)) {
            return expression;
        }
        
        // 去掉开头的 ${ 和结尾的 }
        const content = expression.slice(2, -1).trim();
        
        // 验证表达式内容不为空
        if (!content) {
            console.warn(`[AmisCards.StatRenderer] 空的表达式: ${expression}`);
            return '0';
        }
        
        return content;
    }

    /**
     * 构建描述信息
     * @returns {Object} 描述区域配置
     */
    buildDescription() {
        return {
            type: 'container',
            className: 'amis-cards-stat-description',
            body: [
                {
                    type: 'tpl',
                    tpl: this.dataConfig.description,
                    className: 'stat-description'
                }
            ]
        };
    }

    /**
     * 格式化数值
     * @param {number} value 原始数值
     * @returns {string} 格式化后的数值
     */
    formatValue(value) {
        if (this.formatter && typeof this.formatter === 'function') {
            return this.formatter(value);
        }

        if (this.formatter && typeof this.formatter === 'string') {
            // 预定义的格式化类型
            switch (this.formatter) {
                case 'currency':
                    return window.AmisCards.Utils.formatNumber(value, {
                        decimals: 2,
                        prefix: '¥',
                        thousandsSeparator: ','
                    });
                case 'percentage':
                    return window.AmisCards.Utils.formatNumber(value, {
                        decimals: 0,
                        suffix: '%'
                    });
                case 'decimal':
                    return window.AmisCards.Utils.formatNumber(value, {
                        decimals: 2,
                        thousandsSeparator: ','
                    });
                case 'integer':
                    return window.AmisCards.Utils.formatNumber(value, {
                        decimals: 0,
                        thousandsSeparator: ','
                    });
                case 'fileSize':
                    return window.AmisCards.Utils.formatFileSize(value);
                default:
                    return value;
            }
        }

        // 默认格式化：大数值使用千分位分隔符
        if (typeof value === 'number' && value >= 1000) {
            return window.AmisCards.Utils.formatNumber(value, {
                decimals: 0,
                thousandsSeparator: ','
            });
        }

        return value.toString();
    }

    /**
     * 格式化趋势值
     * @param {number} value 趋势值
     * @param {boolean} percentage 是否为百分比
     * @returns {string} 格式化后的趋势值
     */
    formatTrendValue(value, percentage = false) {
        if (percentage) {
            return `${value}%`;
        }

        if (Math.abs(value) >= 1000) {
            return window.AmisCards.Utils.formatNumber(value, {
                decimals: 0,
                thousandsSeparator: ','
            });
        }

        return value.toString();
    }

    /**
     * 获取趋势图标
     * @param {string} direction 趋势方向
     * @returns {string} 图标类名
     */
    getTrendIcon(direction) {
        switch (direction) {
            case 'up':
                return 'fa-arrow-up';
            case 'down':
                return 'fa-arrow-down';
            case 'stable':
                return 'fa-minus';
            default:
                return 'fa-minus';
        }
    }

    /**
     * 获取趋势颜色
     * @param {string} direction 趋势方向
     * @returns {string} 颜色值
     */
    getTrendColor(direction) {
        switch (direction) {
            case 'up':
                return '#52c41a'; // 绿色
            case 'down':
                return '#ff4d4f'; // 红色
            case 'stable':
                return '#1890ff'; // 蓝色
            default:
                return '#666';
        }
    }

    /**
     * 计算进度百分比
     * @returns {number} 进度百分比
     */
    calculateProgress() {
        if (!this.target || this.target === 0) return 0;

        const percentage = (this.value / this.target) * 100;
        // 四舍五入到2位小数，避免显示过多小数位
        const roundedPercentage = Math.round(percentage * 100) / 100;
        return Math.min(100, Math.max(0, roundedPercentage));
    }

    /**
     * 获取进度条颜色
     * @param {number} percentage 进度百分比
     * @returns {string} 颜色值
     */
    getProgressColor(percentage) {
        if (percentage >= 100) return '#52c41a'; // 绿色
        if (percentage >= 80) return '#1890ff';  // 蓝色
        if (percentage >= 60) return '#faad14';  // 橙色
        return '#ff4d4f'; // 红色
    }

    /**
     * 更新统计数据
     * @param {Object} newData 新数据
     */
    updateStats(newData) {
        // 更新数值
        if (newData.value !== undefined) {
            this.value = newData.value;
        }

        // 更新标签
        if (newData.label !== undefined) {
            this.label = newData.label;
        }

        // 更新趋势
        if (newData.trend !== undefined) {
            this.trend = newData.trend;
        }

        // 更新目标值
        if (newData.target !== undefined) {
            this.target = newData.target;
        }

        // 更新数据配置
        Object.assign(this.dataConfig, newData);

        console.log(`[AmisCards.StatRenderer] 已更新统计数据: ${this.id}`);
    }

    /**
     * 加载数据后的处理
     * @returns {Promise} 处理Promise
     */
    async loadData() {
        const data = await super.loadData();

        if (data) {
            // 如果API返回了数据，更新统计信息
            this.updateStats(data);
        }

        return data;
    }

    /**
     * 构建设置表单（扩展基类）
     * @returns {Object} 设置表单配置
     */
    buildSettingsForm() {
        const baseForm = super.buildSettingsForm();

        // 添加统计卡片特有的设置
        const statSettings = [
            {
                type: 'divider',
                title: '统计设置'
            },
            {
                type: 'input-number',
                name: 'value',
                label: '数值',
                value: this.value
            },
            {
                type: 'input-text',
                name: 'label',
                label: '标签',
                value: this.label
            },
            {
                type: 'input-text',
                name: 'unit',
                label: '单位',
                value: this.unit
            },
            {
                type: 'input-text',
                name: 'prefix',
                label: '前缀',
                value: this.prefix
            },
            {
                type: 'input-text',
                name: 'suffix',
                label: '后缀',
                value: this.suffix
            },
            {
                type: 'select',
                name: 'formatter',
                label: '数值格式',
                value: this.formatter,
                options: [
                    { label: '默认', value: '' },
                    { label: '货币', value: 'currency' },
                    { label: '百分比', value: 'percentage' },
                    { label: '小数', value: 'decimal' },
                    { label: '整数', value: 'integer' },
                    { label: '文件大小', value: 'fileSize' }
                ]
            },
            {
                type: 'switch',
                name: 'animateValue',
                label: '数值动画',
                value: this.animateValue
            },
            {
                type: 'input-number',
                name: 'animationDuration',
                label: '动画时长(毫秒)',
                value: this.animationDuration,
                visibleOn: 'this.animateValue'
            },
            {
                type: 'divider',
                title: '趋势设置'
            },
            {
                type: 'combo',
                name: 'trend',
                label: '趋势信息',
                value: this.trend,
                items: [
                    {
                        type: 'select',
                        name: 'direction',
                        label: '趋势方向',
                        options: [
                            { label: '上升', value: 'up' },
                            { label: '下降', value: 'down' },
                            { label: '稳定', value: 'stable' }
                        ]
                    },
                    {
                        type: 'input-number',
                        name: 'value',
                        label: '趋势值'
                    },
                    {
                        type: 'input-text',
                        name: 'period',
                        label: '时间周期'
                    },
                    {
                        type: 'switch',
                        name: 'percentage',
                        label: '百分比显示'
                    }
                ]
            },
            {
                type: 'divider',
                title: '进度设置'
            },
            {
                type: 'switch',
                name: 'showProgress',
                label: '显示进度条',
                value: this.showProgress
            },
            {
                type: 'input-number',
                name: 'target',
                label: '目标值',
                value: this.target,
                visibleOn: 'this.showProgress'
            }
        ];

        // 插入到基础设置之后
        baseForm.body.splice(-1, 0, ...statSettings);

        return baseForm;
    }

    /**
     * 获取调试信息
     * @returns {Object} 调试信息
     */
    getDebugInfo() {
        const baseInfo = super.getDebugInfo();

        return {
            ...baseInfo,
            statData: {
                value: this.value,
                label: this.label,
                unit: this.unit,
                prefix: this.prefix,
                suffix: this.suffix,
                trend: this.trend,
                target: this.target,
                formatter: this.formatter,
                progress: this.showProgress ? this.calculateProgress() : null
            }
        };
    }
}

// 导出统计卡片渲染器
window.AmisCards.StatRenderer = StatRenderer;

// 自动注册到全局系统
window.AmisCards.registerRenderer = window.AmisCards.registerRenderer || function (type, renderer) {
    console.log(`[AmisCards] 全局注册渲染器: ${type}`);
    // 将渲染器存储到全局对象中
    window.AmisCards.renderers = window.AmisCards.renderers || new Map();
    window.AmisCards.renderers.set(type, renderer);
};

// 注册统计渲染器
window.AmisCards.registerRenderer('stat', StatRenderer);

console.log('[AmisCards.StatRenderer] 统计卡片渲染器已加载'); 