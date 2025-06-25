/**
 * AmisCards 布局通用脚本
 * @description 提供 AmisCards 布局的通用工具函数和配置管理
 * @version 1.0.0
 */

(function() {
    'use strict';

    /**
     * AmisCards 布局通用工具函数
     */
    window.AmisCardsLayout = {
        
        /**
         * 显示加载状态
         * @param {string} message 加载消息
         * @param {string} containerId 容器ID
         */
        showLoading: function(message = '正在加载...', containerId = 'amis-cards-root') {
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = `
                    <div class="amis-cards-loading">
                        <div class="spinner">
                            <i class="fa fa-spinner fa-spin"></i>
                        </div>
                        <div>${this.escapeHtml(message)}</div>
                    </div>
                `;
            }
        },
        
        /**
         * 显示错误信息
         * @param {string} title 错误标题
         * @param {string} message 错误消息
         * @param {string} containerId 容器ID
         */
        showError: function(title = '加载失败', message = '请检查网络连接或重新加载页面', containerId = 'amis-cards-root') {
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = `
                    <div class="amis-cards-error">
                        <i class="fa fa-exclamation-triangle"></i>
                        <div style="font-size: 1.2rem; font-weight: 600; margin-bottom: 0.5rem;">${this.escapeHtml(title)}</div>
                        <div style="font-size: 0.9rem; margin-bottom: 1rem; opacity: 0.8;">${this.escapeHtml(message)}</div>
                        <button class="cxd-Button cxd-Button--danger" onclick="location.reload()">重新加载</button>
                    </div>
                `;
            }
        },
        
        /**
         * 显示成功信息
         * @param {string} title 成功标题
         * @param {string} message 成功消息
         * @param {number} autoHide 自动隐藏时间(毫秒)，0表示不自动隐藏
         * @param {string} containerId 容器ID
         */
        showSuccess: function(title = '操作成功', message = '', autoHide = 3000, containerId = 'amis-cards-root') {
            const container = document.getElementById(containerId);
            if (container) {
                const successHtml = `
                    <div class="amis-cards-success" style="
                        background: rgba(40, 167, 69, 0.1);
                        border: 2px solid rgba(40, 167, 69, 0.3);
                        border-radius: 1rem;
                        padding: 3rem;
                        text-align: center;
                        color: #ffffff;
                        margin: 2rem;
                    ">
                        <i class="fa fa-check-circle" style="color: #28a745; margin-bottom: 1rem; font-size: 2.5rem;"></i>
                        <div style="font-size: 1.2rem; font-weight: 600; margin-bottom: 0.5rem;">${this.escapeHtml(title)}</div>
                        ${message ? `<div style="font-size: 0.9rem; opacity: 0.8;">${this.escapeHtml(message)}</div>` : ''}
                    </div>
                `;
                container.innerHTML = successHtml;
                
                if (autoHide > 0) {
                    setTimeout(() => {
                        if (container.innerHTML === successHtml) {
                            this.showLoading('正在加载...', containerId);
                        }
                    }, autoHide);
                }
            }
        },
        
        /**
         * 切换主题
         * @param {string} theme 主题名称
         */
        switchTheme: function(theme) {
            const body = document.body;
            const currentTheme = body.className.match(/amis-cards-theme-(\w+)/);
            
            if (currentTheme) {
                body.classList.remove(`amis-cards-theme-${currentTheme[1]}`);
            }
            
            body.classList.add(`amis-cards-theme-${theme}`);
            
            if (window.AmisCardsConfig) {
                window.AmisCardsConfig.theme = theme;
            }
            
            console.log('[AmisCards布局] 主题已切换到:', theme);
        },
        
        /**
         * 切换全屏模式
         */
        toggleFullscreen: function() {
            if (!window.AmisCardsConfig?.enableFullscreen) {
                console.warn('[AmisCards布局] 全屏模式未启用');
                return;
            }
            
            const body = document.body;
            const isFullscreen = body.classList.contains('fullscreen-mode');
            
            if (isFullscreen) {
                body.classList.remove('fullscreen-mode');
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                }
                console.log('[AmisCards布局] 已退出全屏模式');
            } else {
                body.classList.add('fullscreen-mode');
                if (document.documentElement.requestFullscreen) {
                    document.documentElement.requestFullscreen();
                }
                console.log('[AmisCards布局] 已进入全屏模式');
            }
        },
        
        /**
         * 检查依赖是否加载完成
         * @param {Function} callback 回调函数
         * @param {number} maxChecks 最大检查次数
         */
        checkDependencies: function(callback, maxChecks = 20) {
            let checkCount = 0;
            
            const checkAndInit = () => {
                checkCount++;
                
                const dependencies = [
                    { name: 'Amis', check: () => typeof window.amis !== 'undefined' },
                    { name: 'AmisCards', check: () => typeof window.AmisCards !== 'undefined' }
                ];
                
                const missing = dependencies.filter(dep => !dep.check());
                
                if (missing.length === 0) {
                    console.log('[AmisCards布局] 所有依赖检查通过');
                    callback(true);
                } else if (checkCount < maxChecks) {
                    setTimeout(checkAndInit, 200);
                } else {
                    console.error('[AmisCards布局] 依赖加载超时，缺少:', missing.map(dep => dep.name));
                    callback(false);
                }
            };
            
            checkAndInit();
        },
        
        /**
         * 获取当前主题
         * @returns {string} 当前主题名称
         */
        getCurrentTheme: function() {
            const body = document.body;
            const themeMatch = body.className.match(/amis-cards-theme-(\w+)/);
            return themeMatch ? themeMatch[1] : 'default';
        },
        
        /**
         * 检查是否处于全屏模式
         * @returns {boolean} 是否全屏
         */
        isFullscreen: function() {
            return document.body.classList.contains('fullscreen-mode');
        },
        
        /**
         * 安全的 HTML 转义
         * @param {string} text 需要转义的文本
         * @returns {string} 转义后的文本
         */
        escapeHtml: function(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },
        
        /**
         * 派发自定义事件
         * @param {string} eventName 事件名称
         * @param {Object} detail 事件详情
         */
        dispatchEvent: function(eventName, detail = {}) {
            const event = new CustomEvent(`amis-cards-${eventName}`, {
                detail: detail,
                bubbles: true
            });
            document.dispatchEvent(event);
        },
        
        /**
         * 监听自定义事件
         * @param {string} eventName 事件名称 (不含 amis-cards- 前缀)
         * @param {Function} callback 回调函数
         */
        addEventListener: function(eventName, callback) {
            document.addEventListener(`amis-cards-${eventName}`, callback);
        },
        
        /**
         * 移除事件监听
         * @param {string} eventName 事件名称 (不含 amis-cards- 前缀)
         * @param {Function} callback 回调函数
         */
        removeEventListener: function(eventName, callback) {
            document.removeEventListener(`amis-cards-${eventName}`, callback);
        },
        
        /**
         * 设置容器内容
         * @param {string} containerId 容器ID
         * @param {string} content HTML内容
         */
        setContent: function(containerId, content) {
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = content;
            }
        },
        
        /**
         * 创建 AmisCards 实例的便捷方法
         * @param {Object} config 配置对象
         * @returns {Object} AmisCards 实例
         */
        createInstance: function(config = {}) {
            if (!window.AmisCards) {
                throw new Error('AmisCards 未加载');
            }
            
            const defaultConfig = {
                container: window.AmisCardsConfig?.container || '#amis-cards-root',
                theme: window.AmisCardsConfig?.theme || 'default',
                config: {
                    autoRefresh: window.AmisCardsConfig?.autoRefresh || false,
                    refreshInterval: window.AmisCardsConfig?.refreshInterval || 30000
                }
            };
            
            return window.AmisCards.create(Object.assign(defaultConfig, config));
        },
        
        /**
         * 注册 AmisCards 渲染器
         * @param {Object} instance AmisCards 实例
         */
        registerRenderers: function(instance) {
            if (!instance) {
                console.warn('[AmisCards布局] 无效的实例，无法注册渲染器');
                return;
            }
            
            try {
                if (window.AmisCards.StatRenderer) {
                    instance.registerRenderer('stat', window.AmisCards.StatRenderer);
                    console.log('[AmisCards布局] 已注册 StatRenderer');
                }
                if (window.AmisCards.TableRenderer) {
                    instance.registerRenderer('table', window.AmisCards.TableRenderer);
                    console.log('[AmisCards布局] 已注册 TableRenderer');
                }
                if (window.AmisCards.ChartRenderer) {
                    instance.registerRenderer('chart', window.AmisCards.ChartRenderer);
                    console.log('[AmisCards布局] 已注册 ChartRenderer');
                }
                if (window.AmisCards.InfoRenderer) {
                    instance.registerRenderer('info', window.AmisCards.InfoRenderer);
                    console.log('[AmisCards布局] 已注册 InfoRenderer');
                }
            } catch (error) {
                console.warn('[AmisCards布局] 渲染器注册失败:', error);
            }
        },
        
        /**
         * 等待实例准备就绪
         * @param {Object} instance AmisCards 实例
         * @param {Function} callback 回调函数
         * @param {number} maxChecks 最大检查次数
         */
        waitForInstanceReady: function(instance, callback, maxChecks = 20) {
            let checkCount = 0;
            
            const checkReady = () => {
                checkCount++;
                
                if (instance && instance.isReady && instance.isReady()) {
                    console.log('[AmisCards布局] 实例已准备就绪');
                    callback();
                } else if (checkCount < maxChecks) {
                    setTimeout(checkReady, 100);
                } else {
                    console.log('[AmisCards布局] 等待超时，强制执行回调');
                    callback();
                }
            };
            
            checkReady();
        },
        
        /**
         * 应用主题
         * @param {string} theme 主题名称
         * @param {Element} targetElement 目标元素，默认为 document.body
         */
        applyTheme: function(theme, targetElement = document.body) {
            try {
                if (window.AmisCards.ThemeUtils) {
                    window.AmisCards.ThemeUtils.applyTheme(theme, targetElement);
                } else {
                    // 移除旧主题类
                    const oldThemeClasses = Array.from(targetElement.classList).filter(cls => cls.startsWith('amis-cards-theme-'));
                    oldThemeClasses.forEach(cls => targetElement.classList.remove(cls));
                    
                    // 添加新主题类
                    targetElement.classList.add(`amis-cards-theme-${theme}`);
                }
                console.log('[AmisCards布局] 主题已应用:', theme);
            } catch (error) {
                console.error('[AmisCards布局] 主题应用失败:', error);
            }
        },
        
        /**
         * 切换主题（高级版本）
         * @param {string} currentTheme 当前主题
         * @param {string} newTheme 新主题
         * @param {Element} targetElement 目标元素，默认为 document.body
         * @param {Object} options 切换选项
         * @returns {Promise<void>} 切换完成的 Promise
         */
        switchThemeAdvanced: async function(currentTheme, newTheme, targetElement = document.body, options = {}) {
            const defaultOptions = {
                duration: 300,
                easing: 'ease'
            };
            const finalOptions = Object.assign(defaultOptions, options);
            
            try {
                if (window.AmisCards.ThemeUtils && window.AmisCards.ThemeUtils.switchTheme) {
                    await window.AmisCards.ThemeUtils.switchTheme(currentTheme, newTheme, targetElement, finalOptions);
                } else {
                    // 简单的主题切换
                    targetElement.classList.remove(`amis-cards-theme-${currentTheme}`);
                    targetElement.classList.add(`amis-cards-theme-${newTheme}`);
                }
                console.log('[AmisCards布局] 主题已切换:', currentTheme, '->', newTheme);
            } catch (error) {
                console.error('[AmisCards布局] 主题切换失败:', error);
                throw error;
            }
        },
        
        /**
         * 隐藏加载状态
         * @param {string} containerId 容器ID
         */
        hideLoadingState: function(containerId = 'amis-cards-root') {
            try {
                // 查找并隐藏加载状态元素
                const loadingElements = document.querySelectorAll('.amis-cards-loading, .exam-monitor-loading');
                loadingElements.forEach(element => {
                    if (element) {
                        element.style.display = 'none';
                        element.style.visibility = 'hidden';
                        element.remove(); // 完全移除加载元素
                    }
                });
                
                // 确保容器可见
                const container = document.getElementById(containerId);
                if (container) {
                    container.style.display = 'block';
                    container.style.visibility = 'visible';
                }
                
                console.log('[AmisCards布局] 加载状态已隐藏');
            } catch (error) {
                console.warn('[AmisCards布局] 隐藏加载状态失败:', error);
            }
        },
        
        /**
         * 获取认证Token（依赖 TokenManager）
         * @returns {string} 认证Token
         */
        getAuthToken: function() {
            const token = window.TokenManager ? window.TokenManager.getToken() : localStorage.getItem('token');
            return token ? `Bearer ${token}` : '';
        },
        
        /**
         * 获取认证头信息（依赖 TokenManager）
         * @param {string} tenantId 租户ID（可选）
         * @returns {Object} 请求头对象
         */
        getAuthHeaders: function(tenantId = null) {
            const headers = {
                'X-Forwarded-With': 'CodeSpirit'
            };
            
            // 添加认证Token
            if (window.TokenManager) {
                const authHeaders = window.TokenManager.getAuthHeaders();
                Object.assign(headers, authHeaders);
            } else {
                const token = localStorage.getItem('token');
                if (token) {
                    headers['Authorization'] = `Bearer ${token}`;
                }
            }
            
            // 添加租户ID
            if (tenantId) {
                headers['X-Tenant-Id'] = tenantId;
            }
            
            return headers;
        },
        
        /**
         * 深度合并对象
         * @param {Object} target 目标对象
         * @param {Object} source 源对象
         * @returns {Object} 合并后的对象
         */
        deepMerge: function(target, source) {
            const result = Object.assign({}, target);
            
            for (const key in source) {
                if (source.hasOwnProperty(key)) {
                    if (typeof source[key] === 'object' && source[key] !== null && !Array.isArray(source[key])) {
                        result[key] = this.deepMerge(result[key] || {}, source[key]);
                    } else {
                        result[key] = source[key];
                    }
                }
            }
            
            return result;
        },
        
        /**
         * 实用工具：延迟执行
         * @param {Function} func 要执行的函数
         * @param {number} delay 延迟时间(毫秒)
         * @returns {number} 定时器ID
         */
        delay: function(func, delay) {
            return setTimeout(func, delay);
        },
        
        /**
         * 实用工具：防抖
         * @param {Function} func 要防抖的函数
         * @param {number} wait 等待时间(毫秒)
         * @returns {Function} 防抖后的函数
         */
        debounce: function(func, wait) {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        },
        
        /**
         * 初始化方法 - 设置基础事件监听
         */
        init: function() {
            // 监听全屏状态变化
            document.addEventListener('fullscreenchange', () => {
                const isFullscreen = !!document.fullscreenElement;
                if (!isFullscreen && document.body.classList.contains('fullscreen-mode')) {
                    document.body.classList.remove('fullscreen-mode');
                    this.dispatchEvent('fullscreen-exit');
                }
            });
            
            // 监听键盘事件 (ESC 退出全屏)
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this.isFullscreen()) {
                    this.toggleFullscreen();
                }
            });
            
            console.log('[AmisCards布局] 通用脚本初始化完成');
        }
    };
    
    // 全局错误处理
    window.addEventListener('error', function(e) {
        console.error('[AmisCards布局] JavaScript 错误:', e.error);
    });
    
    // 页面加载完成处理
    document.addEventListener('DOMContentLoaded', function() {
        console.log('[AmisCards布局] 页面加载完成');
        
        // 初始化布局工具
        if (window.AmisCardsLayout) {
            window.AmisCardsLayout.init();
        }
    });
    
    // 模块信息
    window.AmisCardsLayout._version = '1.0.0';
    window.AmisCardsLayout._name = 'AmisCards Layout Utils';
    
})(); 