/**
 * 信息卡片渲染器
 * 支持富文本、列表、描述列表等信息展示
 * 
 * @author CodeSpirit
 * @version 2.0
 */

(function() {
    'use strict';
    
    // 确保命名空间存在
    window.AmisCards = window.AmisCards || {};
    
    /**
     * 信息卡片渲染器类
     * 继承自BaseRenderer，专门用于渲染信息展示卡片
     */
    class InfoRenderer extends window.AmisCards.BaseRenderer {
        /**
         * 构造函数
         * @param {Object} config - 信息配置
         */
        constructor(config) {
            super(config);
            
            // 信息类型映射
            this.infoTypeMap = {
                'text': 'text',
                'html': 'html',
                'tpl': 'tpl',
                'list': 'list',
                'definition': 'definition-list',
                'properties': 'property',
                'json': 'json',
                'markdown': 'markdown'
            };
            
            // 默认样式配置
            this.defaultStyleConfig = {
                spacing: 'normal',
                background: 'white',
                shadow: 'sm'
            };
        }
        
        /**
         * 获取渲染器类型
         * @returns {string} 渲染器类型
         */
        getType() {
            return 'info';
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
                className: `${baseConfig.className} amis-cards-info`
            };
        }

        /**
         * 获取卡片主体内容
         * @returns {Array} Amis 组件配置
         */
        getCardBody() {
            return this.buildInfoContent();
        }
        
        /**
         * 构建信息内容
         * @returns {Array} 信息内容配置数组
         */
        buildInfoContent() {
            const infoType = this.config.infoType || 'text';
            
            switch (infoType) {
                case 'text':
                    return this.buildTextContent();
                case 'html':
                    return this.buildHtmlContent();
                case 'tpl':
                    return this.buildTemplateContent();
                case 'list':
                    return this.buildListContent();
                case 'definition':
                    return this.buildDefinitionContent();
                case 'properties':
                    return this.buildPropertiesContent();
                case 'json':
                    return this.buildJsonContent();
                case 'markdown':
                    return this.buildMarkdownContent();
                default:
                    return this.buildTextContent();
            }
        }
        
        /**
         * 构建文本内容
         * @returns {Array} 文本内容配置
         */
        buildTextContent() {
            return [
                {
                    type: 'plain',
                    className: 'amis-cards-info-text',
                    text: this.config.content || this.config.text || '',
                    placeholder: this.config.placeholder || '暂无内容'
                }
            ];
        }
        
        /**
         * 构建HTML内容
         * @returns {Array} HTML内容配置
         */
        buildHtmlContent() {
            return [
                {
                    type: 'html',
                    className: 'amis-cards-info-html',
                    html: this.config.content || this.config.html || ''
                }
            ];
        }
        
        /**
         * 构建模板内容
         * @returns {Array} 模板内容配置
         */
        buildTemplateContent() {
            return [
                {
                    type: 'tpl',
                    className: 'amis-cards-info-tpl',
                    tpl: this.config.content || this.config.tpl || '',
                    data: this.config.data || {}
                }
            ];
        }
        
        /**
         * 构建列表内容
         * @returns {Array} 列表内容配置
         */
        buildListContent() {
            const listItems = this.config.items || this.config.list || [];
            
            return [
                {
                    type: 'list',
                    className: 'amis-cards-info-list',
                    source: this.config.source || listItems,
                    placeholder: '暂无数据',
                    listItem: {
                        body: [
                            {
                                type: 'tpl',
                                tpl: this.config.itemTemplate || '${label || title || name}',
                                className: this.config.itemClassName || ''
                            }
                        ]
                    }
                }
            ];
        }
        
        /**
         * 构建定义列表内容
         * @returns {Array} 定义列表内容配置
         */
        buildDefinitionContent() {
            const definitions = this.config.definitions || this.config.items || [];
            
            return [
                {
                    type: 'definitions',
                    className: 'amis-cards-info-definitions',
                    source: definitions,
                    placeholder: '暂无数据'
                }
            ];
        }
        
        /**
         * 构建属性列表内容
         * @returns {Array} 属性列表内容配置
         */
        buildPropertiesContent() {
            const properties = this.config.properties || this.config.items || [];
            
            return [
                {
                    type: 'property',
                    className: 'amis-cards-info-properties',
                    title: this.config.propertyTitle || '',
                    source: properties,
                    column: this.config.column || 1,
                    mode: this.config.mode || 'table'
                }
            ];
        }
        
        /**
         * 构建JSON内容
         * @returns {Array} JSON内容配置
         */
        buildJsonContent() {
            return [
                {
                    type: 'json',
                    className: 'amis-cards-info-json',
                    source: this.config.source || this.config.data || {},
                    jsonTheme: this.config.jsonTheme || 'twilight',
                    displayDataTypes: this.config.displayDataTypes !== false,
                    quotesOnKeys: this.config.quotesOnKeys !== false,
                    sortKeys: this.config.sortKeys || false,
                    collapsed: this.config.collapsed || false,
                    collapseStringsAfterLength: this.config.collapseStringsAfterLength || 15
                }
            ];
        }
        
        /**
         * 构建Markdown内容
         * @returns {Array} Markdown内容配置
         */
        buildMarkdownContent() {
            return [
                {
                    type: 'markdown',
                    className: 'amis-cards-info-markdown',
                    value: this.config.content || this.config.markdown || '',
                    name: this.config.name || 'markdown'
                }
            ];
        }
        
        /**
         * 构建卡片样式类
         * @returns {string} 样式类字符串
         */
        buildCardClasses() {
            const classes = super.buildCardClasses();
            const spacing = this.config.spacing || this.defaultStyleConfig.spacing;
            const background = this.config.background || this.defaultStyleConfig.background;
            const shadow = this.config.shadow || this.defaultStyleConfig.shadow;
            
            return `${classes} amis-cards-info amis-cards-spacing-${spacing} amis-cards-bg-${background} amis-cards-shadow-${shadow}`;
        }
        
        /**
         * 获取设置表单配置
         * @returns {Object} 设置表单配置
         */
        getSettingsForm() {
            return {
                type: 'form',
                title: '信息设置',
                body: [
                    ...super.getSettingsForm().body,
                    {
                        type: 'divider',
                        title: '信息配置'
                    },
                    {
                        type: 'select',
                        name: 'infoType',
                        label: '信息类型',
                        value: this.config.infoType || 'text',
                        options: [
                            { label: '纯文本', value: 'text' },
                            { label: 'HTML', value: 'html' },
                            { label: '模板', value: 'tpl' },
                            { label: '列表', value: 'list' },
                            { label: '定义列表', value: 'definition' },
                            { label: '属性列表', value: 'properties' },
                            { label: 'JSON', value: 'json' },
                            { label: 'Markdown', value: 'markdown' }
                        ]
                    },
                    {
                        type: 'textarea',
                        name: 'content',
                        label: '内容',
                        value: this.config.content || '',
                        rows: 6,
                        visibleOn: '${infoType === "text" || infoType === "html" || infoType === "tpl" || infoType === "markdown"}'
                    },
                    {
                        type: 'combo',
                        name: 'items',
                        label: '列表项',
                        visibleOn: '${infoType === "list"}',
                        value: this.config.items || [],
                        multiple: true,
                        multiLine: true,
                        items: [
                            {
                                type: 'input-text',
                                name: 'label',
                                label: '标签',
                                required: true
                            },
                            {
                                type: 'input-text',
                                name: 'value',
                                label: '值'
                            }
                        ]
                    },
                    {
                        type: 'combo',
                        name: 'definitions',
                        label: '定义项',
                        visibleOn: '${infoType === "definition"}',
                        value: this.config.definitions || [],
                        multiple: true,
                        multiLine: true,
                        items: [
                            {
                                type: 'input-text',
                                name: 'term',
                                label: '术语',
                                required: true
                            },
                            {
                                type: 'textarea',
                                name: 'description',
                                label: '描述',
                                required: true
                            }
                        ]
                    },
                    {
                        type: 'combo',
                        name: 'properties',
                        label: '属性项',
                        visibleOn: '${infoType === "properties"}',
                        value: this.config.properties || [],
                        multiple: true,
                        multiLine: true,
                        items: [
                            {
                                type: 'input-text',
                                name: 'label',
                                label: '标签',
                                required: true
                            },
                            {
                                type: 'input-text',
                                name: 'content',
                                label: '内容',
                                required: true
                            }
                        ]
                    },
                    {
                        type: 'json-editor',
                        name: 'data',
                        label: 'JSON数据',
                        visibleOn: '${infoType === "json"}',
                        value: this.config.data || {}
                    },
                    {
                        type: 'divider',
                        title: '样式配置'
                    },
                    {
                        type: 'select',
                        name: 'spacing',
                        label: '间距',
                        value: this.config.spacing || 'normal',
                        options: [
                            { label: '紧凑', value: 'compact' },
                            { label: '标准', value: 'normal' },
                            { label: '宽松', value: 'loose' }
                        ]
                    },
                    {
                        type: 'select',
                        name: 'background',
                        label: '背景',
                        value: this.config.background || 'white',
                        options: [
                            { label: '白色', value: 'white' },
                            { label: '灰色', value: 'gray' },
                            { label: '透明', value: 'transparent' }
                        ]
                    },
                    {
                        type: 'select',
                        name: 'shadow',
                        label: '阴影',
                        value: this.config.shadow || 'sm',
                        options: [
                            { label: '无', value: 'none' },
                            { label: '小', value: 'sm' },
                            { label: '中', value: 'md' },
                            { label: '大', value: 'lg' }
                        ]
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
                infoType: this.config.infoType || 'text',
                hasContent: !!(this.config.content || this.config.text || this.config.html),
                itemsCount: (this.config.items || this.config.list || []).length,
                spacing: this.config.spacing || 'normal',
                background: this.config.background || 'white',
                shadow: this.config.shadow || 'sm'
            };
        }
        
        /**
         * 验证配置
         * @returns {Object} 验证结果
         */
        validateConfig() {
            const result = super.validateConfig();
            const infoType = this.config.infoType || 'text';
            
            // 验证内容
            if (['text', 'html', 'tpl', 'markdown'].includes(infoType)) {
                if (!this.config.content && !this.config.text && !this.config.html && !this.config.markdown) {
                    result.errors.push('缺少内容配置');
                }
            }
            
            // 验证列表项
            if (infoType === 'list') {
                if (!this.config.items && !this.config.list && !this.config.source) {
                    result.errors.push('列表类型需要配置items、list或source');
                }
            }
            
            // 验证定义列表
            if (infoType === 'definition') {
                if (!this.config.definitions && !this.config.items) {
                    result.errors.push('定义列表类型需要配置definitions或items');
                }
            }
            
            // 验证属性列表
            if (infoType === 'properties') {
                if (!this.config.properties && !this.config.items) {
                    result.errors.push('属性列表类型需要配置properties或items');
                }
            }
            
            // 验证JSON数据
            if (infoType === 'json') {
                if (!this.config.source && !this.config.data) {
                    result.errors.push('JSON类型需要配置source或data');
                }
            }
            
            // 验证信息类型
            if (!this.infoTypeMap[infoType]) {
                result.errors.push(`不支持的信息类型: ${infoType}`);
            }
            
            return result;
        }
    }
    
    // 注册到全局命名空间
    window.AmisCards.InfoRenderer = InfoRenderer;
    
    // 注册到全局渲染器
    window.AmisCards.registerRenderer = window.AmisCards.registerRenderer || function(type, renderer) {
        console.log(`[AmisCards] 全局注册渲染器: ${type}`);
        window.AmisCards.renderers = window.AmisCards.renderers || new Map();
        window.AmisCards.renderers.set(type, renderer);
    };
    
    window.AmisCards.registerRenderer('info', InfoRenderer);
    
    console.log('[AmisCards] InfoRenderer 已加载');
    
})(); 