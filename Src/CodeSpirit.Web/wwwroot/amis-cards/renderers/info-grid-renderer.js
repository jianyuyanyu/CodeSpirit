/**
 * 信息网格卡片渲染器
 * 专门用于展示网格化的信息项，每个项包含图标、标签和数值
 * 
 * @author CodeSpirit
 * @version 2.0
 */

(function() {
    'use strict';
    
    // 确保命名空间存在
    window.AmisCards = window.AmisCards || {};
    
    /**
     * 信息网格卡片渲染器类
     * 继承自BaseRenderer，专门用于渲染网格化信息展示卡片
     */
    class InfoGridRenderer extends window.AmisCards.BaseRenderer {
        /**
         * 构造函数
         * @param {Object} config - 信息网格配置
         */
        constructor(config) {
            super(config);
            
            // 默认网格配置
            this.defaultGridConfig = {
                columns: 'auto-fit',
                gap: '1.25rem',
                minItemWidth: '220px', // 提高默认最小宽度，避免文字被截断
                itemPadding: '1.5rem',
                showIcons: true,
                iconPosition: 'left',
                iconSize: 'lg'
            };
            
            // 默认项目配置
            this.defaultItemConfig = {
                iconColor: '#3498db',
                iconBackground: 'rgba(52, 152, 219, 0.1)',
                iconBorder: false,
                valueColor: '#2c3e50',
                labelColor: '#7f8c8d'
            };
        }
        
        /**
         * 获取渲染器类型
         * @returns {string} 渲染器类型
         */
        getType() {
            return 'info-grid';
        }
        
        /**
         * 生成Amis配置
         * @returns {Object} Amis页面配置
         */
        generateAmisConfig() {
            const baseConfig = this.getBaseAmisConfig();
            
            return {
                ...baseConfig,
                body: this.getCardBody(),
                className: `${baseConfig.className} amis-cards-info-grid`
            };
        }
        
        /**
         * 获取卡片主体内容
         * @returns {Array} Amis 组件配置
         */
        getCardBody() {
            return [
                {
                    type: 'tpl',
                    tpl: this.buildGridTemplate(),
                    className: 'amis-cards-info-grid-container'
                }
            ];
        }
        
        /**
         * 构建网格模板
         * @returns {string} HTML模板
         */
        buildGridTemplate() {
            const items = this.config.items || [];
            const gridConfig = { ...this.defaultGridConfig, ...(this.config.grid || {}) };
            
            // 调试输出
            console.log('[InfoGridRenderer] 网格配置:', gridConfig);
            console.log('[InfoGridRenderer] 最小项目宽度:', gridConfig.minItemWidth);
            
            const itemsHtml = items.map(item => this.buildItemTemplate(item, gridConfig)).join('');
            
            const gridStyle = this.buildGridStyle(gridConfig);
            
            // 添加CSS变量以支持动态配置
            const cssVariables = this.buildCssVariables(gridConfig);
            
            console.log('[InfoGridRenderer] CSS变量:', cssVariables);
            
            return `
                <div class="exam-monitor-info info-grid-dynamic" style="${gridStyle}" data-min-width="${gridConfig.minItemWidth}">
                    <style>
                        .info-grid-dynamic {
                            ${cssVariables}
                        }
                    </style>
                    ${itemsHtml}
                </div>
            `;
        }
        
        /**
         * 构建网格样式
         * @param {Object} gridConfig - 网格配置
         * @returns {string} CSS样式字符串
         */
        buildGridStyle(gridConfig) {
            const styles = [
                'display: grid',
                'width: 100%'
            ];
            
            if (gridConfig.columns === 'auto-fit') {
                styles.push(`grid-template-columns: repeat(auto-fit, minmax(${gridConfig.minItemWidth}, 1fr))`);
            } else if (typeof gridConfig.columns === 'number') {
                styles.push(`grid-template-columns: repeat(${gridConfig.columns}, 1fr)`);
            } else if (typeof gridConfig.columns === 'string') {
                styles.push(`grid-template-columns: ${gridConfig.columns}`);
            }
            
            if (gridConfig.gap) {
                styles.push(`gap: ${gridConfig.gap}`);
            }
            
            // 网格布局优化
            styles.push('align-items: start');
            styles.push('justify-items: stretch');
            
            return styles.join('; ');
        }
        
        /**
         * 构建单个项目模板
         * @param {Object} item - 项目配置
         * @param {Object} gridConfig - 网格配置
         * @returns {string} 项目HTML模板
         */
        buildItemTemplate(item, gridConfig) {
            const itemConfig = { ...this.defaultItemConfig, ...item };
            const iconHtml = this.buildIconHtml(itemConfig, gridConfig);
            
            // 构建数值显示，支持表达式
            const valueExpression = item.value || item.content || '';
            const unitHtml = item.unit ? ` ${item.unit}` : '';
            
            return `
                <div class="exam-monitor-info-item">
                    ${iconHtml}
                    <div>
                        <div class="info-label">${item.label || item.title || ''}</div>
                        <div class="info-value ${item.highlight ? 'info-highlight' : ''}">${valueExpression}${unitHtml}</div>
                        ${item.description ? `<div class="info-description">${item.description}</div>` : ''}
                    </div>
                </div>
            `;
        }
        
        /**
         * 构建图标HTML
         * @param {Object} itemConfig - 项目配置
         * @param {Object} gridConfig - 网格配置
         * @returns {string} 图标HTML
         */
        buildIconHtml(itemConfig, gridConfig) {
            if (!gridConfig.showIcons || !itemConfig.icon) {
                return '';
            }
            
            const iconClasses = [
                'fa',
                `fa-${itemConfig.icon}`,
                `fa-${gridConfig.iconSize || 'lg'}`
            ].join(' ');
            
            const iconStyle = this.buildIconStyle(itemConfig);
            
            return `<i class="${iconClasses}" style="${iconStyle}"></i>`;
        }
        
        /**
         * 构建图标样式
         * @param {Object} itemConfig - 项目配置
         * @returns {string} 图标CSS样式
         */
        buildIconStyle(itemConfig) {
            const styles = [];
            
            if (itemConfig.iconColor) {
                styles.push(`color: ${itemConfig.iconColor}`);
            }
            
            if (itemConfig.iconBackground) {
                styles.push(`background-color: ${itemConfig.iconBackground}`);
            }
            
            if (itemConfig.iconBorder) {
                styles.push('border: 1px solid currentColor', 'border-radius: 50%', 'padding: 0.5rem');
            }
            
            return styles.join('; ');
        }
        
        /**
         * 构建CSS变量
         * @param {Object} gridConfig - 网格配置
         * @returns {string} CSS变量定义
         */
        buildCssVariables(gridConfig) {
            const variables = [];
            
            if (gridConfig.minItemWidth) {
                variables.push(`--info-grid-min-width: ${gridConfig.minItemWidth}`);
            }
            
            if (gridConfig.gap) {
                variables.push(`--info-grid-gap: ${gridConfig.gap}`);
            }
            
            if (gridConfig.columns && typeof gridConfig.columns === 'number') {
                variables.push(`--info-grid-columns: ${gridConfig.columns}`);
            }
            
            // 构建动态的 grid-template-columns
            let gridColumns;
            if (gridConfig.columns === 'auto-fit') {
                gridColumns = `repeat(auto-fit, minmax(var(--info-grid-min-width, ${gridConfig.minItemWidth}), 1fr))`;
            } else if (typeof gridConfig.columns === 'number') {
                gridColumns = `repeat(${gridConfig.columns}, 1fr)`;
            } else if (typeof gridConfig.columns === 'string' && gridConfig.columns !== 'auto-fit') {
                gridColumns = gridConfig.columns;
            } else {
                gridColumns = `repeat(auto-fit, minmax(var(--info-grid-min-width, ${gridConfig.minItemWidth}), 1fr))`;
            }
            
            variables.push(`grid-template-columns: ${gridColumns} !important`);
            
            return variables.join('; ') + ';';
        }
        
        /**
         * 验证配置
         * @param {Object} config - 配置对象
         * @returns {boolean} 是否有效
         */
        validateConfig(config) {
            if (!super.validateConfig(config)) {
                return false;
            }
            
            if (!config.items || !Array.isArray(config.items)) {
                console.warn(`[AmisCards] InfoGridRenderer 缺少 items 配置: ${config.id}`);
                return false;
            }
            
            // 验证每个项目的配置
            for (const item of config.items) {
                if (!item.label && !item.title) {
                    console.warn(`[AmisCards] InfoGridRenderer 项目缺少 label 或 title: ${config.id}`);
                    return false;
                }
                
                if (!item.value && !item.content) {
                    console.warn(`[AmisCards] InfoGridRenderer 项目缺少 value 或 content: ${config.id}`);
                    return false;
                }
            }
            
            return true;
        }
        
        /**
         * 获取默认配置
         * @returns {Object} 默认配置
         */
        getDefaultConfig() {
            return {
                ...super.getDefaultConfig(),
                type: 'info-grid',
                items: [],
                grid: this.defaultGridConfig
            };
        }
    }
    
    // 注册渲染器到全局
    window.AmisCards.InfoGridRenderer = InfoGridRenderer;
    
    // 注册到全局渲染器
    window.AmisCards.registerRenderer = window.AmisCards.registerRenderer || function(type, renderer) {
        console.log(`[AmisCards] 全局注册渲染器: ${type}`);
        window.AmisCards.renderers = window.AmisCards.renderers || new Map();
        window.AmisCards.renderers.set(type, renderer);
    };
    
    window.AmisCards.registerRenderer('info-grid', InfoGridRenderer);
    
    console.log('[AmisCards] InfoGridRenderer 已加载');
    
})(); 