/**
 * CodeSpirit Amis Cards V2.0 - 数据服务
 * 负责API数据获取、缓存管理和权限验证
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 数据服务类
 * 提供统一的数据获取、缓存和权限管理功能
 */
class DataService {
    /**
     * 构造函数
     * @param {Object} options 配置选项
     * @param {string} options.baseUrl API基础URL
     * @param {number} options.timeout 请求超时时间（毫秒）
     * @param {boolean} options.enableCache 是否启用缓存
     * @param {number} options.cacheTimeout 缓存超时时间（毫秒）
     */
    constructor(options = {}) {
        this.baseUrl = options.baseUrl || '';
        this.timeout = options.timeout || 30000;
        this.enableCache = options.enableCache !== false;
        this.cacheTimeout = options.cacheTimeout || 5 * 60 * 1000; // 5分钟
        this.cache = new Map();
        this.requestQueue = new Map();
        
        console.log('[AmisCards.DataService] 数据服务初始化完成');
    }

    /**
     * GET请求
     * @param {string} url 请求URL
     * @param {Object} params 查询参数
     * @param {Object} options 请求选项
     * @returns {Promise} 响应数据
     */
    async get(url, params = {}, options = {}) {
        const fullUrl = this.buildUrl(url, params);
        const cacheKey = this.getCacheKey('GET', fullUrl);
        
        // 检查缓存
        if (this.enableCache && !options.skipCache) {
            const cached = this.getFromCache(cacheKey);
            if (cached) {
                console.log(`[AmisCards.DataService] 使用缓存数据: ${url}`);
                return cached;
            }
        }
        
        // 防止重复请求
        if (this.requestQueue.has(cacheKey)) {
            return this.requestQueue.get(cacheKey);
        }
        
        const requestPromise = this.makeRequest('GET', fullUrl, null, options);
        this.requestQueue.set(cacheKey, requestPromise);
        
        try {
            const result = await requestPromise;
            
            // 缓存结果
            if (this.enableCache && !options.skipCache) {
                this.setCache(cacheKey, result);
            }
            
            return result;
        } finally {
            this.requestQueue.delete(cacheKey);
        }
    }

    /**
     * POST请求
     * @param {string} url 请求URL
     * @param {Object} data 请求数据
     * @param {Object} options 请求选项
     * @returns {Promise} 响应数据
     */
    async post(url, data = {}, options = {}) {
        const fullUrl = this.buildUrl(url);
        return this.makeRequest('POST', fullUrl, data, options);
    }

    /**
     * PUT请求
     * @param {string} url 请求URL
     * @param {Object} data 请求数据
     * @param {Object} options 请求选项
     * @returns {Promise} 响应数据
     */
    async put(url, data = {}, options = {}) {
        const fullUrl = this.buildUrl(url);
        return this.makeRequest('PUT', fullUrl, data, options);
    }

    /**
     * DELETE请求
     * @param {string} url 请求URL
     * @param {Object} options 请求选项
     * @returns {Promise} 响应数据
     */
    async delete(url, options = {}) {
        const fullUrl = this.buildUrl(url);
        return this.makeRequest('DELETE', fullUrl, null, options);
    }

    /**
     * 构建完整URL
     * @param {string} url 相对URL
     * @param {Object} params 查询参数
     * @returns {string} 完整URL
     */
    buildUrl(url, params = {}) {
        let fullUrl = url.startsWith('http') ? url : `${this.baseUrl}${url}`;
        
        // 添加查询参数
        const queryString = this.buildQueryString(params);
        if (queryString) {
            fullUrl += (fullUrl.includes('?') ? '&' : '?') + queryString;
        }
        
        return fullUrl;
    }

    /**
     * 构建查询字符串
     * @param {Object} params 参数对象
     * @returns {string} 查询字符串
     */
    buildQueryString(params) {
        const searchParams = new URLSearchParams();
        
        Object.entries(params).forEach(([key, value]) => {
            if (value !== null && value !== undefined) {
                if (Array.isArray(value)) {
                    value.forEach(item => searchParams.append(key, item));
                } else {
                    searchParams.append(key, value);
                }
            }
        });
        
        return searchParams.toString();
    }

    /**
     * 执行HTTP请求
     * @param {string} method HTTP方法
     * @param {string} url 请求URL
     * @param {Object} data 请求数据
     * @param {Object} options 请求选项
     * @returns {Promise} 响应数据
     */
    async makeRequest(method, url, data = null, options = {}) {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.timeout);
        
        try {
            const requestOptions = {
                method,
                headers: this.buildHeaders(options.headers),
                signal: controller.signal
            };
            
            // 添加请求体
            if (data && method !== 'GET') {
                if (data instanceof FormData) {
                    requestOptions.body = data;
                } else {
                    requestOptions.body = JSON.stringify(data);
                    requestOptions.headers['Content-Type'] = 'application/json';
                }
            }
            
            console.log(`[AmisCards.DataService] ${method} ${url}`);
            
            const response = await fetch(url, requestOptions);
            
            // 检查响应状态
            if (!response.ok) {
                await this.handleErrorResponse(response);
            }
            
            // 解析响应
            const result = await this.parseResponse(response);
            
            console.log(`[AmisCards.DataService] ${method} ${url} - 成功`);
            
            return result;
            
        } catch (error) {
            console.error(`[AmisCards.DataService] ${method} ${url} - 失败:`, error);
            throw this.handleRequestError(error);
        } finally {
            clearTimeout(timeoutId);
        }
    }

    /**
     * 构建请求头
     * @param {Object} customHeaders 自定义请求头
     * @returns {Object} 完整的请求头
     */
    buildHeaders(customHeaders = {}) {
        const headers = {
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json',
            ...customHeaders
        };
        
        // 添加认证头
        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        return headers;
    }

    /**
     * 获取认证令牌
     * @returns {string|null} 认证令牌
     */
    getAuthToken() {
        // 集成现有的 TokenManager
        if (window.TokenManager && typeof window.TokenManager.getToken === 'function') {
            return window.TokenManager.getToken();
        }
        
        // 备用方案
        return localStorage.getItem('auth_token') || sessionStorage.getItem('auth_token');
    }

    /**
     * 解析响应
     * @param {Response} response Fetch响应对象
     * @returns {Promise} 解析后的数据
     */
    async parseResponse(response) {
        const contentType = response.headers.get('content-type');
        
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        } else if (contentType && contentType.includes('text/')) {
            return await response.text();
        } else {
            return await response.blob();
        }
    }

    /**
     * 处理错误响应
     * @param {Response} response 错误响应
     */
    async handleErrorResponse(response) {
        let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        
        try {
            const errorData = await response.json();
            if (errorData.message) {
                errorMessage = errorData.message;
            } else if (errorData.error) {
                errorMessage = errorData.error;
            }
        } catch (e) {
            // 忽略JSON解析错误
        }
        
        // 处理特定状态码
        switch (response.status) {
            case 401:
                this.handleUnauthorized();
                throw new Error('未授权访问，请重新登录');
            case 403:
                throw new Error('权限不足');
            case 404:
                throw new Error('请求的资源不存在');
            case 500:
                throw new Error('服务器内部错误');
            default:
                throw new Error(errorMessage);
        }
    }

    /**
     * 处理未授权访问
     */
    handleUnauthorized() {
        // 清除本地令牌
        if (window.TokenManager && typeof window.TokenManager.clearToken === 'function') {
            window.TokenManager.clearToken();
        } else {
            localStorage.removeItem('auth_token');
            sessionStorage.removeItem('auth_token');
        }
        
        // 触发重新登录
        console.warn('[AmisCards.DataService] 检测到未授权访问，需要重新登录');
    }

    /**
     * 处理请求错误
     * @param {Error} error 错误对象
     * @returns {Error} 处理后的错误
     */
    handleRequestError(error) {
        if (error.name === 'AbortError') {
            return new Error('请求超时');
        } else if (!navigator.onLine) {
            return new Error('网络连接已断开');
        } else {
            return error;
        }
    }

    /**
     * 生成缓存键
     * @param {string} method HTTP方法
     * @param {string} url 请求URL
     * @returns {string} 缓存键
     */
    getCacheKey(method, url) {
        return `${method}:${url}`;
    }

    /**
     * 从缓存获取数据
     * @param {string} key 缓存键
     * @returns {*} 缓存的数据或null
     */
    getFromCache(key) {
        const cached = this.cache.get(key);
        if (!cached) return null;
        
        // 检查是否过期
        if (Date.now() - cached.timestamp > this.cacheTimeout) {
            this.cache.delete(key);
            return null;
        }
        
        return cached.data;
    }

    /**
     * 设置缓存
     * @param {string} key 缓存键
     * @param {*} data 要缓存的数据
     */
    setCache(key, data) {
        this.cache.set(key, {
            data,
            timestamp: Date.now()
        });
        
        // 定期清理过期缓存
        this.cleanupCache();
    }

    /**
     * 清理过期缓存
     */
    cleanupCache() {
        const now = Date.now();
        for (const [key, cached] of this.cache.entries()) {
            if (now - cached.timestamp > this.cacheTimeout) {
                this.cache.delete(key);
            }
        }
    }

    /**
     * 清空所有缓存
     */
    clearCache() {
        this.cache.clear();
        console.log('[AmisCards.DataService] 已清空所有缓存');
    }

    /**
     * 获取缓存统计信息
     * @returns {Object} 缓存统计
     */
    getCacheStats() {
        return {
            size: this.cache.size,
            timeout: this.cacheTimeout,
            enabled: this.enableCache
        };
    }
}

// 导出数据服务类
window.AmisCards.DataService = DataService;

// 创建默认数据服务实例
window.AmisCards.dataService = new DataService();

console.log('[AmisCards.DataService] 数据服务模块已加载'); 