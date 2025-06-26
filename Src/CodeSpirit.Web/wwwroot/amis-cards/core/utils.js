/**
 * CodeSpirit Amis Cards V2.0 - 工具函数
 * 提供通用的工具方法和辅助函数
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 工具函数集合
 */
const Utils = {
    /**
     * 深拷贝对象
     * @param {*} obj 要拷贝的对象
     * @returns {*} 拷贝后的对象
     */
    deepClone(obj) {
        if (obj === null || typeof obj !== 'object') {
            return obj;
        }
        
        if (obj instanceof Date) {
            return new Date(obj.getTime());
        }
        
        if (obj instanceof Array) {
            return obj.map(item => this.deepClone(item));
        }
        
        if (typeof obj === 'object') {
            const cloned = {};
            Object.keys(obj).forEach(key => {
                cloned[key] = this.deepClone(obj[key]);
            });
            return cloned;
        }
        
        return obj;
    },

    /**
     * 深度合并对象
     * @param {Object} target 目标对象
     * @param {...Object} sources 源对象
     * @returns {Object} 合并后的对象
     */
    deepMerge(target, ...sources) {
        if (!sources.length) return target;
        const source = sources.shift();

        if (this.isObject(target) && this.isObject(source)) {
            Object.keys(source).forEach(key => {
                if (this.isObject(source[key])) {
                    if (!target[key]) target[key] = {};
                    this.deepMerge(target[key], source[key]);
                } else {
                    target[key] = source[key];
                }
            });
        }

        return this.deepMerge(target, ...sources);
    },

    /**
     * 判断是否为对象
     * @param {*} item 要判断的项
     * @returns {boolean} 是否为对象
     */
    isObject(item) {
        return item && typeof item === 'object' && !Array.isArray(item);
    },

    /**
     * 防抖函数
     * @param {Function} func 要防抖的函数
     * @param {number} wait 等待时间（毫秒）
     * @param {boolean} immediate 是否立即执行
     * @returns {Function} 防抖后的函数
     */
    debounce(func, wait, immediate = false) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                timeout = null;
                if (!immediate) func.apply(this, args);
            };
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(this, args);
        };
    },

    /**
     * 节流函数
     * @param {Function} func 要节流的函数
     * @param {number} limit 限制时间（毫秒）
     * @returns {Function} 节流后的函数
     */
    throttle(func, limit) {
        let inThrottle;
        return function(...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    /**
     * 生成唯一ID
     * @param {string} prefix 前缀
     * @returns {string} 唯一ID
     */
    generateId(prefix = 'amis-cards') {
        return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    },

    /**
     * 格式化数字
     * @param {number} num 数字
     * @param {Object} options 格式化选项
     * @returns {string} 格式化后的字符串
     */
    formatNumber(num, options = {}) {
        const {
            decimals = 0,
            thousandsSeparator = ',',
            decimalSeparator = '.',
            prefix = '',
            suffix = ''
        } = options;

        if (isNaN(num)) return num;

        const fixed = Number(num).toFixed(decimals);
        const [integer, decimal] = fixed.split('.');
        const formattedInteger = integer.replace(/\B(?=(\d{3})+(?!\d))/g, thousandsSeparator);
        
        let result = formattedInteger;
        if (decimals > 0 && decimal) {
            result += decimalSeparator + decimal;
        }
        
        return prefix + result + suffix;
    },

    /**
     * 格式化文件大小
     * @param {number} bytes 字节数
     * @param {number} decimals 小数位数
     * @returns {string} 格式化后的大小
     */
    formatFileSize(bytes, decimals = 2) {
        if (bytes === 0) return '0 B';
        
        const k = 1024;
        const dm = decimals < 0 ? 0 : decimals;
        const sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
        
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    },

    /**
     * 格式化日期
     * @param {Date|string|number} date 日期
     * @param {string} format 格式字符串
     * @returns {string} 格式化后的日期
     */
    formatDate(date, format = 'YYYY-MM-DD HH:mm:ss') {
        const d = new Date(date);
        if (isNaN(d.getTime())) return date;

        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        const hours = String(d.getHours()).padStart(2, '0');
        const minutes = String(d.getMinutes()).padStart(2, '0');
        const seconds = String(d.getSeconds()).padStart(2, '0');

        return format
            .replace('YYYY', year)
            .replace('MM', month)
            .replace('DD', day)
            .replace('HH', hours)
            .replace('mm', minutes)
            .replace('ss', seconds);
    },

    /**
     * 计算相对时间
     * @param {Date|string|number} date 日期
     * @returns {string} 相对时间描述
     */
    getRelativeTime(date) {
        const now = new Date();
        const target = new Date(date);
        const diff = now - target;
        
        const minute = 60 * 1000;
        const hour = minute * 60;
        const day = hour * 24;
        const month = day * 30;
        const year = day * 365;
        
        if (diff < minute) {
            return '刚刚';
        } else if (diff < hour) {
            return Math.floor(diff / minute) + '分钟前';
        } else if (diff < day) {
            return Math.floor(diff / hour) + '小时前';
        } else if (diff < month) {
            return Math.floor(diff / day) + '天前';
        } else if (diff < year) {
            return Math.floor(diff / month) + '个月前';
        } else {
            return Math.floor(diff / year) + '年前';
        }
    },

    /**
     * 颜色工具
     */
    color: {
        /**
         * 将十六进制颜色转换为RGB
         * @param {string} hex 十六进制颜色
         * @returns {Object} RGB对象
         */
        hexToRgb(hex) {
            const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
            return result ? {
                r: parseInt(result[1], 16),
                g: parseInt(result[2], 16),
                b: parseInt(result[3], 16)
            } : null;
        },

        /**
         * 将RGB转换为十六进制颜色
         * @param {number} r 红色值
         * @param {number} g 绿色值
         * @param {number} b 蓝色值
         * @returns {string} 十六进制颜色
         */
        rgbToHex(r, g, b) {
            return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
        },

        /**
         * 调整颜色亮度
         * @param {string} color 颜色值
         * @param {number} amount 调整量 (-1 到 1)
         * @returns {string} 调整后的颜色
         */
        adjustBrightness(color, amount) {
            const rgb = this.hexToRgb(color);
            if (!rgb) return color;
            
            const adjust = (value) => {
                const adjusted = Math.round(value + (255 * amount));
                return Math.max(0, Math.min(255, adjusted));
            };
            
            return this.rgbToHex(
                adjust(rgb.r),
                adjust(rgb.g),
                adjust(rgb.b)
            );
        }
    },

    /**
     * DOM工具
     */
    dom: {
        /**
         * 添加CSS类
         * @param {HTMLElement} element 元素
         * @param {string} className 类名
         */
        addClass(element, className) {
            if (element && className) {
                element.classList.add(className);
            }
        },

        /**
         * 移除CSS类
         * @param {HTMLElement} element 元素
         * @param {string} className 类名
         */
        removeClass(element, className) {
            if (element && className) {
                element.classList.remove(className);
            }
        },

        /**
         * 切换CSS类
         * @param {HTMLElement} element 元素
         * @param {string} className 类名
         */
        toggleClass(element, className) {
            if (element && className) {
                element.classList.toggle(className);
            }
        },

        /**
         * 检查是否包含CSS类
         * @param {HTMLElement} element 元素
         * @param {string} className 类名
         * @returns {boolean} 是否包含
         */
        hasClass(element, className) {
            return element && className && element.classList.contains(className);
        },

        /**
         * 获取元素位置
         * @param {HTMLElement} element 元素
         * @returns {Object} 位置信息
         */
        getPosition(element) {
            if (!element) return { top: 0, left: 0, width: 0, height: 0 };
            
            const rect = element.getBoundingClientRect();
            return {
                top: rect.top + window.pageYOffset,
                left: rect.left + window.pageXOffset,
                width: rect.width,
                height: rect.height
            };
        }
    },

    /**
     * 验证工具
     */
    validate: {
        /**
         * 验证邮箱
         * @param {string} email 邮箱地址
         * @returns {boolean} 是否有效
         */
        email(email) {
            const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return regex.test(email);
        },

        /**
         * 验证手机号
         * @param {string} phone 手机号
         * @returns {boolean} 是否有效
         */
        phone(phone) {
            const regex = /^1[3-9]\d{9}$/;
            return regex.test(phone);
        },

        /**
         * 验证URL
         * @param {string} url URL地址
         * @returns {boolean} 是否有效
         */
        url(url) {
            try {
                new URL(url);
                return true;
            } catch {
                return false;
            }
        },

        /**
         * 验证是否为空
         * @param {*} value 值
         * @returns {boolean} 是否为空
         */
        isEmpty(value) {
            if (value === null || value === undefined) return true;
            if (typeof value === 'string') return value.trim() === '';
            if (Array.isArray(value)) return value.length === 0;
            if (typeof value === 'object') return Object.keys(value).length === 0;
            return false;
        }
    },

    /**
     * 存储工具
     */
    storage: {
        /**
         * 设置本地存储
         * @param {string} key 键
         * @param {*} value 值
         * @param {number} expiry 过期时间（毫秒）
         */
        setLocal(key, value, expiry = null) {
            const data = {
                value,
                expiry: expiry ? Date.now() + expiry : null
            };
            localStorage.setItem(`amis-cards-${key}`, JSON.stringify(data));
        },

        /**
         * 获取本地存储
         * @param {string} key 键
         * @returns {*} 值
         */
        getLocal(key) {
            try {
                const item = localStorage.getItem(`amis-cards-${key}`);
                if (!item) return null;
                
                const data = JSON.parse(item);
                if (data.expiry && Date.now() > data.expiry) {
                    localStorage.removeItem(`amis-cards-${key}`);
                    return null;
                }
                
                return data.value;
            } catch {
                return null;
            }
        },

        /**
         * 移除本地存储
         * @param {string} key 键
         */
        removeLocal(key) {
            localStorage.removeItem(`amis-cards-${key}`);
        },

        /**
         * 设置会话存储
         * @param {string} key 键
         * @param {*} value 值
         */
        setSession(key, value) {
            sessionStorage.setItem(`amis-cards-${key}`, JSON.stringify(value));
        },

        /**
         * 获取会话存储
         * @param {string} key 键
         * @returns {*} 值
         */
        getSession(key) {
            try {
                const item = sessionStorage.getItem(`amis-cards-${key}`);
                return item ? JSON.parse(item) : null;
            } catch {
                return null;
            }
        },

        /**
         * 移除会话存储
         * @param {string} key 键
         */
        removeSession(key) {
            sessionStorage.removeItem(`amis-cards-${key}`);
        }
    },

    /**
     * 事件工具
     */
    event: {
        /**
         * 触发自定义事件
         * @param {HTMLElement} element 元素
         * @param {string} eventName 事件名
         * @param {*} detail 事件详情
         */
        trigger(element, eventName, detail = null) {
            const event = new CustomEvent(eventName, { 
                detail, 
                bubbles: true, 
                cancelable: true 
            });
            element.dispatchEvent(event);
        },

        /**
         * 添加事件监听器
         * @param {HTMLElement} element 元素
         * @param {string} eventName 事件名
         * @param {Function} handler 处理函数
         * @param {Object} options 选项
         */
        on(element, eventName, handler, options = {}) {
            if (element && eventName && typeof handler === 'function') {
                element.addEventListener(eventName, handler, options);
            }
        },

        /**
         * 移除事件监听器
         * @param {HTMLElement} element 元素
         * @param {string} eventName 事件名
         * @param {Function} handler 处理函数
         */
        off(element, eventName, handler) {
            if (element && eventName && typeof handler === 'function') {
                element.removeEventListener(eventName, handler);
            }
        }
    }
};

// 导出工具函数
window.AmisCards.Utils = Utils;

console.log('[AmisCards.Utils] 工具函数模块已加载'); 