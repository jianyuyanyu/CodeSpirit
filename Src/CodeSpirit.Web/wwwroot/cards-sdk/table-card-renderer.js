/**
 * CodeSpirit Cards SDK - 表格卡片渲染器
 * 基于 Amis Table 组件的表格卡片实现
 * @version 1.0.2
 * @author CodeSpirit Team
 * 
 * 修复说明 v1.0.2:
 * - 修复了Table组件不支持filter、headerToolbar、footerToolbar属性的问题
 * - 静态数据始终使用Table组件，仅支持基础表格功能
 * - API数据使用CRUD组件，完全支持过滤器、工具栏等高级功能
 * - 遵循Amis官方文档规范，确保组件配置正确性
 */

/**
 * 表格卡片渲染器
 * 支持基于 Amis Table 组件的表格展示
 */
class TableCardRenderer {
    constructor() {
        this.amisInstance = null;
        this.tableInstances = new Map();
    }

    /**
     * 渲染表格卡片
     * @param {Object} config 表格配置
     * @returns {HTMLElement} 渲染后的表格卡片元素
     */
    async render(config) {
        try {
            // 创建卡片容器
            const card = this.createCardContainer(config);
            
            // 创建表格内容
            const tableContainer = this.createTableContainer(config);
            card.appendChild(tableContainer);
            
            // 延迟渲染Amis表格，确保元素已添加到DOM
            setTimeout(async () => {
                try {
                    await this.renderAmisTable(tableContainer, config);
                } catch (error) {
                    console.error('延迟渲染Amis表格失败:', error);
                    // 在表格容器中显示错误信息
                    const tableContent = tableContainer.querySelector('.table-content');
                    if (tableContent) {
                        tableContent.innerHTML = `
                            <div class="alert alert-danger">
                                <strong>表格渲染失败：</strong> ${error.message}
                            </div>
                        `;
                    }
                }
            }, 0);

            return card;
        } catch (error) {
            console.error('表格卡片渲染失败:', error);
            return this.renderErrorCard(config, error);
        }
    }

    /**
     * 更新表格数据
     * @param {HTMLElement} element 表格元素
     * @param {Object} config 新配置
     */
    async update(element, config) {
        try {
            const tableContainer = element.querySelector('.table-content');
            if (tableContainer) {
                // 清空容器
                tableContainer.innerHTML = '';
                
                // 重新渲染表格
                await this.renderAmisTable(tableContainer, config);
            }
        } catch (error) {
            console.error('表格更新失败:', error);
        }
    }

    /**
     * 创建卡片容器
     * @param {Object} config 配置
     * @returns {HTMLElement} 卡片容器元素
     */
    createCardContainer(config) {
        const card = document.createElement('div');
        card.className = `card table-card ${this.getThemeClass(config.style?.theme)}`;
        card.setAttribute('data-card-id', config.id);

        // 设置卡片样式
        if (config.style?.height) {
            card.style.height = `${config.style.height}px`;
        }

        // 创建卡片头部
        if (config.title || config.subtitle) {
            const header = this.createCardHeader(config);
            card.appendChild(header);
        }

        return card;
    }

    /**
     * 创建卡片头部
     * @param {Object} config 配置
     * @returns {HTMLElement} 头部元素
     */
    createCardHeader(config) {
        const header = document.createElement('div');
        header.className = 'card-header';

        if (config.title) {
            const title = document.createElement('h5');
            title.className = 'card-title mb-0';
            title.textContent = config.title;
            header.appendChild(title);
        }

        if (config.subtitle) {
            const subtitle = document.createElement('p');
            subtitle.className = 'card-subtitle text-muted mt-1 mb-0';
            subtitle.textContent = config.subtitle;
            header.appendChild(subtitle);
        }

        return header;
    }

    /**
     * 创建表格容器
     * @param {Object} config 配置
     * @returns {HTMLElement} 表格容器元素
     */
    createTableContainer(config) {
        const container = document.createElement('div');
        container.className = 'card-body p-0';

        const tableContent = document.createElement('div');
        tableContent.className = 'table-content';
        container.appendChild(tableContent);

        return container;
    }

    /**
     * 渲染 Amis 表格
     * @param {HTMLElement} container 容器元素
     * @param {Object} config 表格配置
     */
    async renderAmisTable(container, config) {
        const amisConfig = this.buildAmisTableConfig(config);
        
        try {
            // 找到实际的表格内容容器
            const tableContent = container.querySelector('.table-content');
            if (!tableContent) {
                throw new Error('未找到表格内容容器');
            }
            
            // 检查是否包装在 Service 中
            const finalConfig = config.wrapWithService 
                ? this.wrapWithService(amisConfig, config)
                : amisConfig;

            // 渲染 Amis 组件
            const amisInstance = await this.renderAmisComponent(tableContent, finalConfig, config);
            
            // 存储实例引用
            this.tableInstances.set(config.id, amisInstance);
            
            console.log('Amis 表格渲染成功:', config.id);
        } catch (error) {
            console.error('Amis 表格渲染失败:', error);
            throw error;
        }
    }

    /**
     * 构建 Amis 表格配置
     * 根据Amis官方文档：
     * - 静态数据只能使用Table或Table2组件，不支持filter、headerToolbar、footerToolbar属性
     * - CRUD组件支持完整功能（filter、headerToolbar、footerToolbar等），但只适用于API数据源
     * @param {Object} config 原始配置
     * @returns {Object} Amis 表格配置
     */
    buildAmisTableConfig(config) {
        const { data } = config;
        
        // 判断是否使用静态数据 - 根据Amis官方文档规范
        const useStaticData = !data.api && (!data.source || data.source === '${items}');
        // 检查是否有过滤器配置
        const hasFilter = data.filter && data.filter !== null;
        
        let tableConfig;
        
        if (useStaticData) {
            // 静态数据只能使用Table组件，不支持过滤器
            console.log(`📦 使用静态数据Table模式 [${config.id}]`);
            
            // 检查不支持的配置，提供警告和建议
            if (hasFilter) {
                console.warn(`⚠️ 静态数据不支持Amis原生filter，已忽略filter配置 [${config.id}]`);
            }
            
            if (data.headerToolbar && data.headerToolbar.length > 0) {
                console.warn(`⚠️ Table组件不支持headerToolbar，已忽略headerToolbar配置 [${config.id}]`);
            }
            
            if (data.footerToolbar && data.footerToolbar.length > 0) {
                console.warn(`⚠️ Table组件不支持footerToolbar，已忽略footerToolbar配置 [${config.id}]`);
            }
            
            if (hasFilter || (data.headerToolbar && data.headerToolbar.length > 0) || (data.footerToolbar && data.footerToolbar.length > 0)) {
                console.log(`💡 建议: 需要过滤器或工具栏功能时，请考虑：
                1. 使用API数据源（CRUD组件完全支持这些功能）
                2. 在数据源侧预处理数据
                3. 考虑将静态数据改为API接口提供`);
            }
            
            tableConfig = {
                type: 'table',
                className: 'table-card-table table-responsive',
                // 对于静态数据，使用source指向上下文中的items
                source: '${items}',
                affixHeader: false,
                
                // 列配置 - 确保与Amis官方规范一致
                columns: this.processColumns(data.columns || [])
            };
            
            // 选择配置
            if (data.selectable) {
                tableConfig.selectable = true;
                tableConfig.multiple = data.multiple !== false;
            }
            
            // 展开行配置
            if (data.expandable) {
                tableConfig.expandable = true;
                if (data.expandedRowRender) {
                    tableConfig.expandedRowRender = data.expandedRowRender;
                }
            }
            
            // 表格样式配置
            if (data.bordered !== false) {
                tableConfig.bordered = true;
            }
            
            if (data.striped !== false) {
                tableConfig.striped = true;  
            }
            
            if (data.hover !== false) {
                tableConfig.hover = true;
            }
            
            // 行样式配置
            if (data.rowClassName) {
                tableConfig.rowClassName = data.rowClassName;
            }
            
            if (data.rowClassNameExpr) {
                tableConfig.rowClassNameExpr = data.rowClassNameExpr;
            }
        } else {
            // 使用CRUD组件处理API数据
            console.log(`🌐 使用API数据模式 [${config.id}]`);
            tableConfig = {
                type: 'crud',
                className: 'table-card-crud',
                syncLocation: false,
                
                // 分页配置
                perPage: data.perPage || 10,
                perPageAvailable: data.perPageAvailable || [10, 20, 50, 100],
                
                // 列配置
                columns: this.processColumns(data.columns || []),
                
                // 过滤器配置
                filter: data.filter || null,
                
                // 头部工具栏
                headerToolbar: data.headerToolbar || ['pagination'],
                
                // 底部工具栏
                footerToolbar: data.footerToolbar || ['pagination']
            };
            
            // 添加 API 配置
            if (data.api) {
                tableConfig.api = data.api;
            }
            
            // 处理数据源配置
            if (data.source && data.source !== '${items}') {
                tableConfig.source = data.source;
            }
            
            // 选择配置
            if (data.selectable) {
                tableConfig.selectable = true;
                tableConfig.multiple = data.multiple !== false;
            }
            
            // 展开行配置
            if (data.expandable) {
                tableConfig.expandable = true;
                if (data.expandedRowRender) {
                    tableConfig.expandedRowRender = data.expandedRowRender;
                }
            }
            
            // 搜索配置
            if (data.searchable) {
                tableConfig.searchable = true;
                if (data.searchConfig) {
                    tableConfig.searchConfig = data.searchConfig;
                }
            }
            
            // 排序配置
            if (data.sortable !== false) {
                tableConfig.sortable = true;
            }
            
            // 表格样式配置
            if (data.bordered !== false) {
                tableConfig.bordered = true;
            }
            
            if (data.striped !== false) {
                tableConfig.striped = true;
            }
            
            if (data.hover !== false) {
                tableConfig.hover = true;
            }
            
            // 行样式配置
            if (data.rowClassName) {
                tableConfig.rowClassName = data.rowClassName;
            }
            
            if (data.rowClassNameExpr) {
                tableConfig.rowClassNameExpr = data.rowClassNameExpr;
            }
        }

        return tableConfig;
    }

    /**
     * 处理列配置
     * @param {Array} columns 原始列配置
     * @returns {Array} 处理后的列配置
     */
    processColumns(columns) {
        return columns.map(column => {
            const processedColumn = { ...column };

            // 处理特殊列类型
            switch (column.type) {
                case 'status':
                    processedColumn.type = 'mapping';
                    processedColumn.map = column.map || this.getDefaultStatusMap();
                    break;
                    
                case 'progress':
                    processedColumn.type = 'progress';
                    processedColumn.showLabel = column.showLabel !== false;
                    break;
                    
                case 'datetime':
                    processedColumn.type = 'datetime';
                    processedColumn.format = column.format || 'YYYY-MM-DD HH:mm:ss';
                    break;
                    
                case 'number':
                    processedColumn.type = 'text';
                    if (column.precision !== undefined) {
                        processedColumn.tpl = `\${${column.name} | number:${column.precision}}`;
                    }
                    break;
                    
                case 'operation':
                    processedColumn.type = 'operation';
                    processedColumn.buttons = column.buttons || [];
                    break;
            }

            return processedColumn;
        });
    }

    /**
     * 获取默认状态映射
     * @returns {Object} 状态映射配置
     */
    getDefaultStatusMap() {
        return {
            'active': '<span class="label label-success">激活</span>',
            'inactive': '<span class="label label-default">未激活</span>',
            'online': '<span class="label label-success">在线</span>',
            'offline': '<span class="label label-danger">离线</span>',
            'submitted': '<span class="label label-info">已提交</span>',
            'pending': '<span class="label label-warning">待处理</span>'
        };
    }

    /**
     * 使用 Service 包装表格
     * @param {Object} tableConfig 表格配置
     * @param {Object} config 原始配置
     * @returns {Object} Service 包装后的配置
     */
    wrapWithService(tableConfig, config) {
        const serviceConfig = config.serviceConfig || {};
        
        // 根据Amis Service组件规范构建配置
        const wrappedConfig = {
            type: 'service',
            body: [tableConfig]
        };
        
        // 添加API配置
        if (serviceConfig.api || config.data.api) {
            wrappedConfig.api = serviceConfig.api || config.data.api;
        }
        
        // 添加轮询配置
        if (serviceConfig.interval || config.refreshInterval) {
            wrappedConfig.interval = serviceConfig.interval || config.refreshInterval;
        }
        
        // 添加初始数据
        if (config.data.initialData) {
            wrappedConfig.data = config.data.initialData;
        }
        
        return wrappedConfig;
    }

    /**
     * 渲染 Amis 组件
     * @param {HTMLElement} container 容器
     * @param {Object} schema Amis 配置
     * @param {Object} config 原始配置
     * @returns {Object} Amis 实例
     */
    async renderAmisComponent(container, schema, config) {
        if (window.amisRequire && typeof (window.amis) === "undefined") {
            window.amis = window.amisRequire('amis/embed');
        }
        
        // 使用 amis.embed（需要ID选择器）
        if (window.amis && typeof window.amis.embed === 'function') {
            // 确保容器有ID，amis.embed只支持ID选择器
            let containerId = container.id;
            if (!containerId) {
                containerId = 'amis-table-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                container.id = containerId;
            }
            
            // 检查容器是否已添加到DOM中
            if (!document.body.contains(container)) {
                console.warn('容器尚未添加到DOM中，等待下一个事件循环');
                await new Promise(resolve => setTimeout(resolve, 0));
                
                // 再次检查
                if (!document.body.contains(container)) {
                    throw new Error('容器未添加到DOM中，无法渲染Amis组件');
                }
            }
            
            // 对于静态数据的Table组件，直接在schema中设置data
            const useStaticData = !config.data?.api && (!config.data?.source || config.data?.source === '${items}');
            if (useStaticData && schema.type === 'table') {
                let staticData = [];
                
                // 获取静态数据
                if (config.data?.data && Array.isArray(config.data.data)) {
                    staticData = config.data.data;
                } else if (config.data?.initialData?.items && Array.isArray(config.data.initialData.items)) {
                    staticData = config.data.initialData.items;
                } else if (config.data?.items && Array.isArray(config.data.items)) {
                    staticData = config.data.items;
                }
                
                // 直接在schema中设置data属性，格式为包含items的对象
                schema.data = {
                    items: staticData
                };
                // 移除source属性
                delete schema.source;
                
                console.log(`🔧 为Table组件直接设置data属性 [${config.id}]:`, {
                    dataStructure: schema.data,
                    itemsLength: staticData.length,
                    firstItem: staticData[0]
                });
            }
            
            // 打印AMIS配置（开发模式）
            if (config.debug !== false) {
                console.group(`🎯 AMIS表格配置 [${config.id}]`);
                console.log('📋 完整Schema配置:', JSON.stringify(schema, null, 2));
                console.log('🎨 原始卡片配置:', config);
                console.log('📍 容器ID:', containerId);
                console.groupEnd();
            }
            
            // 构建数据上下文和处理器配置
            const { amisOptions, amisHandlers } = this.getAmisOptionsAndHandlers(config);
            
            console.log(`🚀 开始调用amis.embed [${config.id}]:`, {
                selector: '#' + containerId,
                schema: schema,
                options: amisOptions,
                handlers: amisHandlers,
                hasItems: !!(amisOptions.data && amisOptions.data.items),
                itemsCount: amisOptions.data?.items?.length || 0,
                hasSchemaData: !!(schema.data),
                schemaDataItemsLength: schema.data?.items?.length || 0
            });
            
            const amisInstance = window.amis.embed('#' + containerId, schema, amisOptions, amisHandlers);
            
            console.log(`✅ amis.embed 调用完成 [${config.id}]:`, amisInstance);
            
            return amisInstance;
        }
        
        throw new Error('无法找到可用的 Amis 渲染方法');
    }

    /**
     * 获取AMIS数据上下文和处理器配置
     * @param {Object} config 卡片配置
     * @returns {Object} 包含amisOptions和amisHandlers的对象
     */
    getAmisOptionsAndHandlers(config) {
        // 获取SDK实例的全局配置
        const globalConfig = window.CodeSpiritCards?.SDK?.prototype?.globalAmisConfig || this.globalAmisConfig || {};
        
        // 判断是否使用静态数据 - 与buildAmisTableConfig保持一致
        const useStaticData = !config.data?.api && (!config.data?.source || config.data?.source === '${items}');
        
        console.log(`📊 数据模式判断 [${config.id}]:`, {
            useStaticData,
            hasFilter: !!(config.data?.filter),
            hasApi: !!config.data?.api,
            source: config.data?.source,
            hasStaticData: !!(config.data?.data && Array.isArray(config.data.data)),
            staticDataLength: config.data?.data?.length || 0,
            componentType: useStaticData ? 'Table' : 'CRUD',
            filterSupported: !useStaticData
        });
        
        // 构建数据上下文 - 第三个参数
        const amisOptions = {
            // 位置信息
            location: window.location,
            
            // 全局数据 - 根据Amis规范构建数据上下文
            data: {
                // 添加一些通用的全局变量
                currentTime: new Date().toISOString(),
                cardId: config.id,
                cardTitle: config.title,
                // 合并初始数据（但排除items，因为我们会单独处理）
                ...(config.data?.initialData ? 
                    Object.fromEntries(
                        Object.entries(config.data.initialData).filter(([key]) => key !== 'items')
                    ) : {}
                )
            },
            
            // 其他上下文信息
            context: {
                cardId: config.id,
                cardTitle: config.title
            }
        };
        
        // 如果是静态数据模式，将静态数据添加到items字段
        if (useStaticData) {
            let staticData = [];
            
            // 检查多种可能的静态数据位置
            if (config.data?.data && Array.isArray(config.data.data)) {
                staticData = config.data.data;
                console.log(`📦 从config.data.data获取静态数据 [${config.id}]:`, staticData.length, '条记录');
            } else if (config.data?.initialData?.items && Array.isArray(config.data.initialData.items)) {
                staticData = config.data.initialData.items;
                console.log(`📦 从config.data.initialData.items获取静态数据 [${config.id}]:`, staticData.length, '条记录');
            } else if (config.data?.items && Array.isArray(config.data.items)) {
                staticData = config.data.items;
                console.log(`📦 从config.data.items获取静态数据 [${config.id}]:`, staticData.length, '条记录');
            } else {
                console.warn(`⚠️ 静态数据模式但未找到有效数据 [${config.id}]`);
            }
            
            // 对于Table组件，将静态数据设置为组件的直接数据源
            amisOptions.data.items = staticData;
            
            console.log(`🔍 设置静态数据到amisOptions.data.items [${config.id}]:`, {
                itemsLength: staticData.length,
                firstItem: staticData[0],
                amisOptionsData: amisOptions.data
            });
        } else {
            amisOptions.data.items = [];
            console.log(`🌐 API数据模式，items初始化为空数组 [${config.id}]`);
        }
        
        // 构建处理器配置 - 第四个参数
        const amisHandlers = {
            // 主题配置 - 优先级：卡片配置 > 全局配置 > 默认值
            theme: config.theme || globalConfig.theme || 'antd',
            
            // 请求适配器 - 统一处理API请求
            requestAdaptor: (api) => {
                console.log(`🚀 API请求 [${config.id}]:`, api);
                
                // 调用全局请求拦截器
                if (globalConfig.requestInterceptor && typeof globalConfig.requestInterceptor === 'function') {
                    api = globalConfig.requestInterceptor(api, config) || api;
                }
                
                // 添加通用请求头
                api.headers = {
                    'Content-Type': 'application/json',
                    'X-Card-ID': config.id,
                    'X-Card-Type': 'table',
                    'X-SDK-Version': '1.0.0',
                    ...api.headers
                };
                
                // 添加认证信息 - 必须使用TokenManager
                if (!window.TokenManager) {
                    throw new Error('TokenManager is required but not found. Please ensure TokenManager.js is loaded before Cards SDK.');
                }
                
                const authHeaders = window.TokenManager.getAuthHeaders();
                Object.assign(api.headers, authHeaders);
                
                // 添加平台信息
                const platformInfo = window.TokenManager.getPlatformInfo();
                if (platformInfo) {
                    if (platformInfo.tenantId) {
                        api.headers['X-Tenant-ID'] = platformInfo.tenantId;
                    }
                    if (platformInfo.clientType) {
                        api.headers['X-Client-Type'] = platformInfo.clientType;
                    }
                }
                
                // 添加当前租户ID（如果有）
                if (window.TokenManager.currentTenantId) {
                    api.headers['X-Tenant-ID'] = window.TokenManager.currentTenantId;
                }
                
                // 添加客户端类型（如果有）
                if (window.TokenManager.currentClientType) {
                    api.headers['X-Client-Type'] = window.TokenManager.currentClientType;
                }
                
                // 处理API URL
                if (api.url && !api.url.startsWith('http')) {
                    // 如果是相对路径，添加基础URL
                    const baseUrl = config.apiBaseUrl || globalConfig.apiBaseUrl || '/api';
                    api.url = `${baseUrl}${api.url.startsWith('/') ? '' : '/'}${api.url}`;
                }
                
                console.log(`📤 处理后的API请求:`, api);
                return api;
            },
            
            // 响应适配器 - 统一处理API响应
            responseAdaptor: (api, payload, query, request, response) => {
                console.log(`📥 API响应 [${config.id}]:`, {
                    api: api.url,
                    status: response?.status,
                    payload
                });
                
                // 调用全局响应拦截器
                if (globalConfig.responseInterceptor && typeof globalConfig.responseInterceptor === 'function') {
                    const interceptedPayload = globalConfig.responseInterceptor(api, payload, query, request, response, config);
                    if (interceptedPayload !== undefined) {
                        payload = interceptedPayload;
                    }
                }
                
                // 处理认证失败
                if (payload?.status === 401 || response?.status === 401) {
                    console.warn('🔐 认证失败，需要重新登录');
                    
                    // 基于TokenManager清除认证信息
                    if (!window.TokenManager) {
                        throw new Error('TokenManager is required but not found. Cannot handle authentication failure.');
                    }
                    
                    const platformName = window.TokenManager.getClientTypeName ? 
                        window.TokenManager.getClientTypeName() : '系统';
                    console.warn(`🔐 ${platformName}认证失败，清除Token`);
                    window.TokenManager.clearToken();
                    
                    // 调用全局错误处理器
                    if (globalConfig.errorHandler && typeof globalConfig.errorHandler === 'function') {
                        globalConfig.errorHandler('AUTH_FAILED', { api, payload, response }, config);
                    }
                    
                    return {
                        status: 1,
                        msg: '认证失败，请重新登录',
                        data: {}
                    };
                }
                
                // 处理服务器错误
                if (response?.status >= 500) {
                    console.error('🚨 服务器错误:', response);
                    // 调用全局错误处理器
                    if (globalConfig.errorHandler && typeof globalConfig.errorHandler === 'function') {
                        globalConfig.errorHandler('SERVER_ERROR', { api, payload, response }, config);
                    }
                    return {
                        status: 1,
                        msg: '服务器错误，请稍后重试',
                        data: {}
                    };
                }
                
                // 标准化响应格式 - 确保符合Amis期望的数据格式
                if (payload && typeof payload === 'object') {
                    // 如果响应格式不标准，进行转换
                    if (payload.code !== undefined && payload.status === undefined) {
                        payload.status = payload.code === 200 ? 0 : 1;
                    }
                    
                    // 确保有msg字段
                    if (!payload.msg && payload.message) {
                        payload.msg = payload.message;
                    }
                    
                    // 确保数据结构符合Amis期望
                    if (payload.data && Array.isArray(payload.data.items)) {
                        // 如果数据在data.items中，保持结构
                    } else if (Array.isArray(payload.data)) {
                        // 如果data直接是数组，包装成items结构
                        payload.data = {
                            items: payload.data,
                            total: payload.total || payload.data.length
                        };
                    } else if (Array.isArray(payload)) {
                        // 如果payload直接是数组，包装成标准结构
                        payload = {
                            status: 0,
                            msg: 'success',
                            data: {
                                items: payload,
                                total: payload.length
                            }
                        };
                    }
                }
                
                return payload;
            },
            
            // 错误处理
            errorAdaptor: (api, response, query, request) => {
                console.error(`❌ API错误 [${config.id}]:`, {
                    api: api.url,
                    response,
                    query
                });
                
                return {
                    status: 1,
                    msg: response?.message || '请求失败',
                    data: {}
                };
            },
            
            // 国际化配置
            locale: config.locale || globalConfig.locale || 'zh-CN',
            
            // 其他选项
            affixOffsetTop: 0,
            affixOffsetBottom: 0,
            richTextToken: '',
            
            // 自定义函数
            getModalContainer: () => document.body,
            
            // 调试模式
            debug: config.debug !== false && globalConfig.debug !== false
        };
        
        // 合并用户自定义选项
        if (config.amisOptions) {
            Object.assign(amisOptions, config.amisOptions.data ? { data: { ...amisOptions.data, ...config.amisOptions.data } } : {});
            Object.assign(amisHandlers, config.amisOptions.handlers || {});
        }
        
        console.log(`⚙️ AMIS配置 [${config.id}]:`, {
            options: amisOptions,
            handlers: amisHandlers
        });
        
        return { amisOptions, amisHandlers };
    }

    /**
     * 获取主题样式类
     * @param {string} theme 主题名称
     * @returns {string} 样式类名
     */
    getThemeClass(theme = 'default') {
        const themeMap = {
            'default': 'theme-default',
            'primary': 'theme-primary',
            'success': 'theme-success',
            'warning': 'theme-warning',
            'danger': 'theme-danger',
            'info': 'theme-info'
        };
        return themeMap[theme] || themeMap.default;
    }

    /**
     * 渲染错误卡片
     * @param {Object} config 配置
     * @param {Error} error 错误对象
     * @returns {HTMLElement} 错误卡片元素
     */
    renderErrorCard(config, error) {
        const card = document.createElement('div');
        card.className = 'card table-card error-card';
        
        card.innerHTML = `
            <div class="card-header">
                <h5 class="card-title text-danger">
                    <i class="fas fa-exclamation-triangle"></i>
                    ${config.title || '表格加载失败'}
                </h5>
            </div>
            <div class="card-body">
                <div class="alert alert-danger">
                    <strong>错误信息：</strong> ${error.message}
                </div>
                <button class="btn btn-outline-primary" onclick="location.reload()">
                    <i class="fas fa-refresh"></i> 重新加载
                </button>
            </div>
        `;
        
        return card;
    }

    /**
     * 销毁表格实例
     * @param {string} cardId 卡片ID
     */
    destroy(cardId) {
        const instance = this.tableInstances.get(cardId);
        if (instance && typeof instance.unmount === 'function') {
            instance.unmount();
        }
        this.tableInstances.delete(cardId);
    }
}

// 导出渲染器到全局作用域
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TableCardRenderer;
} else if (typeof window !== 'undefined') {
    // 直接注册到window对象 - cards-sdk.js期望的格式
    window.TableCardRenderer = TableCardRenderer;
    
    // 同时注册到CodeSpiritCards命名空间
    if (!window.CodeSpiritCards) {
        window.CodeSpiritCards = {};
    }
    window.CodeSpiritCards.TableCardRenderer = TableCardRenderer;
    
    console.log('✅ TableCardRenderer已注册到全局作用域');
} 