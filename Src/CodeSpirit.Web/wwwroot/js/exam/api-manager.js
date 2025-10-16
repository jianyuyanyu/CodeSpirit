/**
 * 考试系统API请求管理器
 * 负责处理API地址转换和统一的请求处理
 * @module ExamApiManager
 */
(function() {
    'use strict';

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
                
                // 处理HTTP错误
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                
                // 解析响应
                const result = await response.json();
                
                // 处理业务错误
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
        }
    };

    // 向后兼容：提供全局的apiRequest函数
    window.apiRequest = function(url, options = {}) {
        return window.ExamApiManager.request(url, options);
    };

    console.log('[ExamApiManager] API管理器已初始化');
    
})();
