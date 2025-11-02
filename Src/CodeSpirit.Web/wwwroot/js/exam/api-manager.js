/**
 * 考试系统API请求管理器
 * 负责处理API地址转换和统一的请求处理
 * @module ExamApiManager
 */
(function() {
    'use strict';

    /**
     * 缓存管理器
     */
    const CacheManager = {
        /**
         * 获取缓存的key
         * @param {string} key - 基础key
         * @param {string} tenantId - 租户ID
         * @returns {string} 完整的缓存key
         */
        getCacheKey: function(key, tenantId) {
            return `exam_cache_${tenantId}_${key}`;
        },

        /**
         * 获取缓存数据
         * @param {string} key - 缓存key
         * @param {string} tenantId - 租户ID
         * @returns {Object|null} 缓存的数据，如果不存在或已过期返回null
         */
        get: function(key, tenantId) {
            try {
                const cacheKey = this.getCacheKey(key, tenantId);
                const cached = sessionStorage.getItem(cacheKey);
                if (!cached) return null;

                const data = JSON.parse(cached);
                
                // 检查是否过期（默认30分钟）
                if (data.expiry && Date.now() > data.expiry) {
                    sessionStorage.removeItem(cacheKey);
                    return null;
                }
                
                return data.value;
            } catch (error) {
                console.error('[缓存读取失败]', error);
                return null;
            }
        },

        /**
         * 设置缓存数据
         * @param {string} key - 缓存key
         * @param {string} tenantId - 租户ID
         * @param {Object} value - 要缓存的数据
         * @param {number} ttl - 过期时间（毫秒），默认30分钟
         */
        set: function(key, tenantId, value, ttl = 30 * 60 * 1000) {
            try {
                const cacheKey = this.getCacheKey(key, tenantId);
                const data = {
                    value: value,
                    expiry: ttl ? Date.now() + ttl : null
                };
                sessionStorage.setItem(cacheKey, JSON.stringify(data));
            } catch (error) {
                console.error('[缓存写入失败]', error);
            }
        },

        /**
         * 清除指定租户的所有缓存
         * @param {string} tenantId - 租户ID
         */
        clearTenantCache: function(tenantId) {
            try {
                const prefix = `exam_cache_${tenantId}_`;
                const keys = Object.keys(sessionStorage);
                keys.forEach(key => {
                    if (key.startsWith(prefix)) {
                        sessionStorage.removeItem(key);
                    }
                });
                console.log(`[缓存清理] 已清除租户 ${tenantId} 的所有缓存`);
            } catch (error) {
                console.error('[缓存清理失败]', error);
            }
        },

        /**
         * 清除所有缓存
         */
        clearAll: function() {
            try {
                const keys = Object.keys(sessionStorage);
                keys.forEach(key => {
                    if (key.startsWith('exam_cache_')) {
                        sessionStorage.removeItem(key);
                    }
                });
                console.log('[缓存清理] 已清除所有考试缓存');
            } catch (error) {
                console.error('[缓存清理失败]', error);
            }
        }
    };

    /**
     * API地址管理器
     */
    window.ExamApiManager = {
        
        /**
         * 转换API URL
         * 根据站点配置决定是否使用直连API还是代理
         * @param {string} url - 原始URL，格式如 /exam/api/xxx
         * @returns {string} 转换后的URL
         */
        transformUrl: function(url) {
            // 检查是否设置了API基础地址
            const apiBaseUrl = window.siteSettings?.apiBaseUrl;
            
            if (!apiBaseUrl || apiBaseUrl.trim() === '') {
                // 未设置API基础地址，使用原有代理方式
                return url;
            }
            
            // 设置了API基础地址，进行转换
            // 将 /exam/api/xxx 转换为 {apiBaseUrl}/api/xxx
            if (url.startsWith('/exam/api/')) {
                // 移除 /exam 前缀，保留 /api/ 部分
                const apiPath = url.substring('/exam'.length);
                return apiBaseUrl.replace(/\/$/, '') + apiPath;
            }
            
            // 如果不是考试API路径，直接返回原URL
            return url;
        },

        /**
         * 统一的API请求函数
         * @param {string} url - API路径
         * @param {Object} options - fetch选项
         * @returns {Promise} API响应数据
         */
        request: async function(url, options = {}) {
            try {
                // 转换URL
                const transformedUrl = this.transformUrl(url);
                
                // 获取认证token
                const token = window.TokenManager?.getToken();
                
                // 构建请求配置
                const requestConfig = {
                    ...options,
                    headers: {
                        'Authorization': token ? 'Bearer ' + token : '',
                        'TenantId': window.tenantId,
                        'X-Forwarded-With': 'CodeSpirit',
                        'Content-Type': 'application/json',
                        ...options.headers
                    }
                };

                console.log(`[API请求] 原始URL: ${url}, 转换后URL: ${transformedUrl}`);
                
                // 发送请求
                const response = await fetch(transformedUrl, requestConfig);
                
                // 处理认证失败
                if (response.status === 401) {
                    window.location.href = `/${window.tenantId}/exam/login`;
                    throw new Error('认证失败，请重新登录');
                }
                
                // 处理HTTP错误（尝试读取响应体中的业务错误消息）
                if (!response.ok) {
                    let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
                    
                    // 尝试读取响应体中的JSON错误消息
                    try {
                        const contentType = response.headers.get('content-type');
                        
                        // 如果是JSON格式，尝试解析业务错误消息
                        if (contentType && contentType.includes('application/json')) {
                            const errorResult = await response.json();
                            
                            // 如果响应包含业务错误消息，使用它
                            if (errorResult && typeof errorResult === 'object') {
                                if (errorResult.msg) {
                                    errorMessage = errorResult.msg;
                                } else if (errorResult.message) {
                                    errorMessage = errorResult.message;
                                } else if (errorResult.status !== undefined && errorResult.status !== 0) {
                                    // 如果有status但不为0，至少使用status和默认消息
                                    errorMessage = errorResult.msg || `请求失败 (状态码: ${errorResult.status})`;
                                }
                            }
                        }
                    } catch (parseError) {
                        // 如果解析失败，使用默认的HTTP错误消息
                        console.warn('无法解析错误响应体:', parseError);
                    }
                    
                    // 创建错误对象，包含业务错误消息
                    const error = new Error(errorMessage);
                    // 附加原始HTTP信息，方便调试
                    error.httpStatus = response.status;
                    error.httpStatusText = response.statusText;
                    throw error;
                }
                
                // 解析响应
                const result = await response.json();
                
                // 处理业务错误（正常情况下，成功响应但status不为0）
                if (result.status !== undefined && result.status !== 0) {
                    throw new Error(result.msg || '请求失败');
                }
                
                // 返回数据
                return result.data || result;
                
            } catch (error) {
                console.error(`[API请求失败] URL: ${url}`, error);
                throw error;
            }
        },

        /**
         * GET请求
         * @param {string} url - API路径
         * @param {Object} options - 额外选项
         * @returns {Promise} API响应数据
         */
        get: function(url, options = {}) {
            return this.request(url, { ...options, method: 'GET' });
        },

        /**
         * POST请求
         * @param {string} url - API路径
         * @param {Object} data - 请求数据
         * @param {Object} options - 额外选项
         * @returns {Promise} API响应数据
         */
        post: function(url, data = null, options = {}) {
            const requestOptions = { ...options, method: 'POST' };
            if (data) {
                requestOptions.body = JSON.stringify(data);
            }
            return this.request(url, requestOptions);
        },

        /**
         * PUT请求
         * @param {string} url - API路径
         * @param {Object} data - 请求数据
         * @param {Object} options - 额外选项
         * @returns {Promise} API响应数据
         */
        put: function(url, data = null, options = {}) {
            const requestOptions = { ...options, method: 'PUT' };
            if (data) {
                requestOptions.body = JSON.stringify(data);
            }
            return this.request(url, requestOptions);
        },

        /**
         * DELETE请求
         * @param {string} url - API路径
         * @param {Object} options - 额外选项
         * @returns {Promise} API响应数据
         */
        delete: function(url, options = {}) {
            return this.request(url, { ...options, method: 'DELETE' });
        },

        /**
         * 获取租户登录配置（带缓存）
         * @param {string} tenantId - 租户ID
         * @param {boolean} forceRefresh - 是否强制刷新缓存
         * @returns {Promise<Object>} 登录配置数据
         */
        getLoginConfig: async function(tenantId, forceRefresh = false) {
            const cacheKey = 'login_config';
            
            // 检查缓存
            if (!forceRefresh) {
                const cached = CacheManager.get(cacheKey, tenantId);
                if (cached) {
                    console.log(`[登录配置] 使用缓存数据 (租户: ${tenantId})`);
                    return cached;
                }
            }
            
            // 缓存未命中或强制刷新，请求API
            console.log(`[登录配置] 请求API数据 (租户: ${tenantId})`);
            try {
                const url = `/identity/api/identity/tenants/${tenantId}/login-config`;
                
                // 发送请求（login-config 接口无需认证）
                const response = await fetch(url, {
                    method: 'GET',
                    headers: {
                        'TenantId': tenantId,
                        'X-Forwarded-With': 'CodeSpirit',
                        'Content-Type': 'application/json'
                    }
                });
                
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                
                const result = await response.json();
                const data = result.data || result;
                
                // 缓存数据（2小时）
                CacheManager.set(cacheKey, tenantId, data, 2 * 60 * 60 * 1000);
                
                return data;
            } catch (error) {
                console.error('[登录配置获取失败]', error);
                throw error;
            }
        },

        /**
         * 清除缓存
         * @param {string} tenantId - 可选，指定租户ID则只清除该租户的缓存
         */
        clearCache: function(tenantId = null) {
            if (tenantId) {
                CacheManager.clearTenantCache(tenantId);
            } else {
                CacheManager.clearAll();
            }
        }
    };

    // 向后兼容：提供全局的apiRequest函数
    window.apiRequest = function(url, options = {}) {
        return window.ExamApiManager.request(url, options);
    };

    console.log('[ExamApiManager] API管理器已初始化');
    
})();
