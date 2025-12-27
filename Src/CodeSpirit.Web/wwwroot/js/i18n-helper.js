/**
 * CodeSpirit 前端国际化辅助类
 */
(function() {
    'use strict';

    // 初始化全局命名空间
    window.CodeSpirit = window.CodeSpirit || {};

    /**
     * 国际化辅助对象
     */
    window.CodeSpirit.i18n = {
        // 当前语言（从服务器端注入）
        currentLanguage: 'zh-CN',
        
        // 语言资源（从服务器端注入）
        resources: {},
        
        /**
         * 获取翻译文本
         * @param {string} key - 资源键
         * @param {object} params - 参数对象，如 {0: 'value1', 1: 'value2'} 或 {userName: 'John'}
         * @returns {string} 翻译后的文本
         */
        t: function(key, params) {
            params = params || {};
            let text = this.resources[key] || key;
            
            // 替换参数占位符
            if (typeof params === 'object') {
                Object.keys(params).forEach(function(k) {
                    // 支持 {0}, {1} 或 {key} 格式
                    var placeholder = '{' + k + '}';
                    text = text.replace(new RegExp(placeholder.replace(/[{}]/g, '\\$&'), 'g'), params[k]);
                });
            }
            
            return text;
        },
        
        /**
         * 切换语言
         * @param {string} lang - 语言代码（如 'zh-CN', 'en'）
         */
        switchLanguage: function(lang) {
            // 设置 Cookie（格式：c=zh-CN|uic=zh-CN）
            var cookieValue = 'c=' + lang + '|uic=' + lang;
            var expires = new Date();
            expires.setTime(expires.getTime() + (365 * 24 * 60 * 60 * 1000)); // 1年
            document.cookie = '.AspNetCore.Culture=' + cookieValue + '; path=/; expires=' + expires.toUTCString();
            
            // 刷新页面以应用新语言
            location.reload();
        },
        
        /**
         * 获取当前语言
         * @returns {string} 当前语言代码
         */
        getCurrentLanguage: function() {
            return this.currentLanguage;
        },
        
        /**
         * 初始化（从服务器端调用）
         * @param {string} language - 当前语言
         * @param {object} resources - 资源对象
         */
        init: function(language, resources) {
            this.currentLanguage = language || 'zh-CN';
            this.resources = resources || {};
        }
    };
})();
