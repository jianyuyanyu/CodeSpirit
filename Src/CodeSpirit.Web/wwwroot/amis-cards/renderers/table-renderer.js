/**
 * 表格卡片渲染器
 * 支持数据表格展示、搜索、分页、排序等功能
 * 
 * @author CodeSpirit
 * @version 2.0
 */

(function() {
    'use strict';
    
    // 确保命名空间存在
    window.AmisCards = window.AmisCards || {};
    
    /**
     * 表格卡片渲染器类
     * 继承自BaseRenderer，专门用于渲染数据表格卡片
     */
    class TableRenderer extends window.AmisCards.BaseRenderer {
        /**
         * 构造函数
         * @param {Object} config - 表格配置
         */
        constructor(config) {
            super(config);
            
            // 默认表格配置
            this.defaultTableConfig = {
                striped: true,
                bordered: false,
                size: 'sm',
                resizable: true,
                columnsTogglable: false,
                footable: true
            };
            
            // 默认分页配置
            this.defaultPageConfig = {
                layout: ['pager', 'perpage', 'total'],
                perPageAvailable: [10, 20, 50, 100],
                perPage: 20,
                maxButtons: 7,
                showPerPage: true,
                showPageInput: true
            };
        }
        
        /**
         * 获取渲染器类型
         * @returns {string} 渲染器类型
         */
        getType() {
            return 'table';
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
                className: `${baseConfig.className} amis-cards-table`
            };
        }
        
        /**
         * 获取卡片主体内容
         * @returns {Array} Amis 组件配置
         */
        getCardBody() {
            return [
                // 搜索工具栏
                this.buildSearchToolbar(),
                // 表格主体
                this.buildTableConfig()
            ].filter(Boolean);
        }
        
        /**
         * 构建搜索工具栏
         * @returns {Object|null} 搜索工具栏配置
         */
        buildSearchToolbar() {
            if (!this.config.showSearch) {
                return null;
            }
            
            const searchFields = this.config.searchFields || [];
            if (searchFields.length === 0) {
                return null;
            }
            
            return {
                type: 'form',
                className: 'amis-cards-table-search mb-3',
                wrapWithPanel: false,
                mode: 'horizontal',
                target: 'table',
                body: [
                    {
                        type: 'group',
                        body: [
                            ...searchFields.map(field => this.buildSearchField(field)),
                            {
                                type: 'button-group',
                                buttons: [
                                    {
                                        type: 'submit',
                                        label: '搜索',
                                        level: 'primary',
                                        icon: 'fa fa-search'
                                    },
                                    {
                                        type: 'reset',
                                        label: '重置',
                                        icon: 'fa fa-refresh'
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };
        }
        
        /**
         * 构建搜索字段
         * @param {Object} field - 字段配置
         * @returns {Object} 搜索字段配置
         */
        buildSearchField(field) {
            const baseField = {
                type: field.type || 'input-text',
                name: field.name,
                label: field.label,
                placeholder: field.placeholder,
                clearable: true,
                size: 'sm'
            };
            
            // 根据字段类型添加特定配置
            switch (field.type) {
                case 'select':
                    return {
                        ...baseField,
                        options: field.options || [],
                        searchable: field.searchable !== false
                    };
                case 'date':
                    return {
                        ...baseField,
                        type: 'input-date',
                        format: field.format || 'YYYY-MM-DD'
                    };
                case 'date-range':
                    return {
                        ...baseField,
                        type: 'input-date-range',
                        format: field.format || 'YYYY-MM-DD'
                    };
                default:
                    return baseField;
            }
        }
        
        /**
         * 构建表格配置
         * @returns {Object} 表格配置
         */
        buildTableConfig() {
            console.log('[TableRenderer] 构建表格配置，原始配置:', this.config);
            
            const tableConfig = {
                type: 'table',
                name: 'table',
                className: 'amis-cards-table',
                columns: this.buildColumns(),
                ...this.defaultTableConfig,
                ...this.config.tableConfig
            };
            
            // 处理数据源：优先使用静态数据，其次使用API
            if (this.config.data) {
                // 静态数据
                console.log('[TableRenderer] 使用静态数据:', this.config.data);
                tableConfig.data = this.config.data;
            } else if (this.config.api) {
                // API数据
                console.log('[TableRenderer] 使用API数据:', this.config.api);
                tableConfig.api = this.config.api;
            } else if (this.config.data?.api) {
                // 嵌套API配置
                console.log('[TableRenderer] 使用嵌套API数据:', this.config.data.api);
                tableConfig.api = this.config.data.api;
            } else {
                console.warn('[TableRenderer] 没有找到数据源配置');
            }
            
            // 添加分页配置
            if (this.config.showPager !== false) {
                tableConfig.pageField = 'page';
                tableConfig.perPageField = 'perPage';
                tableConfig.perPageAvailable = this.config.perPageAvailable || this.defaultPageConfig.perPageAvailable;
                tableConfig.perPage = this.config.perPage || this.defaultPageConfig.perPage;
                tableConfig.maxButtons = this.config.maxButtons || this.defaultPageConfig.maxButtons;
                tableConfig.showPerPage = this.config.showPerPage !== false;
                tableConfig.showPageInput = this.config.showPageInput !== false;
            }
            
            // 添加工具栏
            if (this.config.tableToolbar && this.config.tableToolbar.length > 0) {
                tableConfig.headerToolbar = [
                    'bulkActions',
                    ...this.config.tableToolbar,
                    'columns-toggler',
                    'pagination'
                ];
            }
            
            // 添加批量操作
            if (this.config.bulkActions && this.config.bulkActions.length > 0) {
                tableConfig.bulkActions = this.config.bulkActions;
            }
            
            // 添加行操作
            if (this.config.rowActions && this.config.rowActions.length > 0) {
                tableConfig.columns.push({
                    type: 'operation',
                    label: '操作',
                    width: this.config.operationWidth || 120,
                    buttons: this.config.rowActions
                });
            }
            
            console.log('[TableRenderer] 最终表格配置:', tableConfig);
            return tableConfig;
        }
        
        /**
         * 构建表格列配置
         * @returns {Array} 表格列配置数组
         */
        buildColumns() {
            const columns = this.config.columns || [];
            
            return columns.map(column => {
                const baseColumn = {
                    name: column.name,
                    label: column.label,
                    type: column.type || 'text',
                    width: column.width,
                    minWidth: column.minWidth,
                    fixed: column.fixed,
                    sortable: column.sortable,
                    searchable: column.searchable,
                    toggled: column.toggled !== false,
                    remark: column.remark,
                    popOver: column.popOver
                };
                
                // 根据列类型添加特定配置
                switch (column.type) {
                    case 'text':
                        return {
                            ...baseColumn,
                            placeholder: column.placeholder || '-',
                            copyable: column.copyable || false
                        };
                        
                    case 'tpl':
                        return {
                            ...baseColumn,
                            tpl: column.tpl
                        };
                        
                    case 'image':
                        return {
                            ...baseColumn,
                            enlargeAble: column.enlargeAble !== false,
                            thumbMode: column.thumbMode || 'cover',
                            thumbRatio: column.thumbRatio || '1:1'
                        };
                        
                    case 'date':
                        return {
                            ...baseColumn,
                            format: column.format || 'YYYY-MM-DD',
                            valueFormat: column.valueFormat || 'X'
                        };
                        
                    case 'datetime':
                        return {
                            ...baseColumn,
                            format: column.format || 'YYYY-MM-DD HH:mm:ss',
                            valueFormat: column.valueFormat || null // 不转换时间戳，直接使用字符串
                        };
                        
                    case 'status':
                        return {
                            ...baseColumn,
                            map: column.statusMap || {
                                1: '<span class="label label-success">正常</span>',
                                0: '<span class="label label-danger">禁用</span>'
                            }
                        };
                        
                    case 'mapping':
                        return {
                            ...baseColumn,
                            map: column.map || {}
                        };
                        
                    case 'progress':
                        return {
                            ...baseColumn,
                            showLabel: column.showLabel !== false,
                            stripe: column.stripe || false
                        };
                        
                    case 'link':
                        return {
                            ...baseColumn,
                            href: column.href,
                            blank: column.blank !== false
                        };
                        
                    case 'button':
                    case 'button-group':
                        return {
                            ...baseColumn,
                            buttons: column.buttons || []
                        };
                        
                    default:
                        return baseColumn;
                }
            });
        }
        
        /**
         * 获取设置表单配置
         * @returns {Object} 设置表单配置
         */
        getSettingsForm() {
            return {
                type: 'form',
                title: '表格设置',
                body: [
                    ...super.getSettingsForm().body,
                    {
                        type: 'divider',
                        title: '表格配置'
                    },
                    {
                        type: 'switch',
                        name: 'showSearch',
                        label: '显示搜索',
                        value: this.config.showSearch || false
                    },
                    {
                        type: 'switch',
                        name: 'showPager',
                        label: '显示分页',
                        value: this.config.showPager !== false
                    },
                    {
                        type: 'number',
                        name: 'perPage',
                        label: '每页条数',
                        value: this.config.perPage || 20,
                        min: 10,
                        max: 100,
                        visibleOn: '${showPager}'
                    },
                    {
                        type: 'switch',
                        name: 'striped',
                        label: '斑马纹',
                        value: this.config.tableConfig?.striped !== false
                    },
                    {
                        type: 'switch',
                        name: 'bordered',
                        label: '边框',
                        value: this.config.tableConfig?.bordered || false
                    },
                    {
                        type: 'select',
                        name: 'size',
                        label: '表格大小',
                        value: this.config.tableConfig?.size || 'sm',
                        options: [
                            { label: '紧凑', value: 'sm' },
                            { label: '标准', value: 'md' },
                            { label: '宽松', value: 'lg' }
                        ]
                    },
                    {
                        type: 'switch',
                        name: 'resizable',
                        label: '可调整列宽',
                        value: this.config.tableConfig?.resizable !== false
                    },
                    {
                        type: 'number',
                        name: 'operationWidth',
                        label: '操作列宽度',
                        value: this.config.operationWidth || 120,
                        min: 80,
                        max: 300,
                        visibleOn: '${rowActions && rowActions.length > 0}'
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
                columnsCount: this.config.columns?.length || 0,
                hasSearch: !!this.config.showSearch,
                hasPager: this.config.showPager !== false,
                perPage: this.config.perPage || 20,
                hasRowActions: !!(this.config.rowActions && this.config.rowActions.length > 0),
                hasBulkActions: !!(this.config.bulkActions && this.config.bulkActions.length > 0)
            };
        }
        
        /**
         * 验证配置
         * @returns {Object} 验证结果
         */
        validateConfig() {
            const result = super.validateConfig();
            
            // 验证数据源
            if (!this.config.data?.api && !this.config.api) {
                result.errors.push('表格需要配置数据源API');
            }
            
            // 验证列配置
            if (!this.config.columns || this.config.columns.length === 0) {
                result.errors.push('表格需要配置至少一列');
            }
            
            // 验证列配置详细信息
            if (this.config.columns) {
                this.config.columns.forEach((column, index) => {
                    if (!column.name) {
                        result.errors.push(`第${index + 1}列缺少name属性`);
                    }
                    if (!column.label) {
                        result.warnings.push(`第${index + 1}列缺少label属性`);
                    }
                });
            }
            
            // 验证搜索字段
            if (this.config.showSearch && this.config.searchFields) {
                this.config.searchFields.forEach((field, index) => {
                    if (!field.name) {
                        result.errors.push(`第${index + 1}个搜索字段缺少name属性`);
                    }
                });
            }
            
            return result;
        }
    }
    
    // 注册到全局命名空间
    window.AmisCards.TableRenderer = TableRenderer;
    
    // 注册到全局渲染器
    window.AmisCards.registerRenderer = window.AmisCards.registerRenderer || function(type, renderer) {
        console.log(`[AmisCards] 全局注册渲染器: ${type}`);
        window.AmisCards.renderers = window.AmisCards.renderers || new Map();
        window.AmisCards.renderers.set(type, renderer);
    };
    
    window.AmisCards.registerRenderer('table', TableRenderer);
    
    console.log('[AmisCards] TableRenderer 已加载');
    
})(); 