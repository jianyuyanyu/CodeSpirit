/**
 * CodeSpirit Amis Cards V2.0 - 主题配置
 * 定义各种主题的颜色配置和样式定义
 * 
 * @version 2.0.0  
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 主题配置定义
 */
const ThemeConfigs = {
    /**
     * 默认主题
     */
    default: {
        name: 'default',
        displayName: '默认主题',
        colors: {
            primary: '#007bff',
            secondary: '#6c757d',
            success: '#28a745',
            info: '#17a2b8',
            warning: '#ffc107',
            danger: '#dc3545',
            light: '#f8f9fa',
            dark: '#343a40',
            white: '#ffffff',
            black: '#000000'
        },
        backgrounds: {
            primary: '#007bff',
            secondary: '#6c757d',
            success: '#28a745',
            info: '#17a2b8', 
            warning: '#ffc107',
            danger: '#dc3545',
            light: '#f8f9fa',
            dark: '#343a40',
            gradient: {
                primary: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                success: 'linear-gradient(135deg, #11998e 0%, #38ef7d 100%)',
                info: 'linear-gradient(135deg, #74b9ff 0%, #0984e3 100%)',
                warning: 'linear-gradient(135deg, #fdcb6e 0%, #e17055 100%)',
                danger: 'linear-gradient(135deg, #fd79a8 0%, #e84393 100%)'
            }
        },
        text: {
            primary: '#212529',
            secondary: '#6c757d',
            muted: '#adb5bd',
            white: '#ffffff',
            light: '#e9ecef'
        },
        borders: {
            color: '#dee2e6',
            width: '1px',
            radius: '0.375rem'
        },
        shadows: {
            sm: '0 0.125rem 0.25rem rgba(0, 0, 0, 0.075)',
            md: '0 0.5rem 1rem rgba(0, 0, 0, 0.15)',
            lg: '0 1rem 3rem rgba(0, 0, 0, 0.175)',
            none: 'none'
        }
    },

    /**
     * 暗色主题
     */
    dark: {
        name: 'dark',
        displayName: '暗色主题',
        colors: {
            primary: '#0d6efd',
            secondary: '#6c757d',
            success: '#198754',
            info: '#0dcaf0',
            warning: '#fd7e14',
            danger: '#dc3545',
            light: '#212529',
            dark: '#f8f9fa',
            white: '#212529',
            black: '#ffffff'
        },
        backgrounds: {
            primary: '#0d6efd',
            secondary: '#6c757d',
            success: '#198754',
            info: '#0dcaf0',
            warning: '#fd7e14',
            danger: '#dc3545',
            light: '#212529',
            dark: '#f8f9fa',
            gradient: {
                primary: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                success: 'linear-gradient(135deg, #11998e 0%, #38ef7d 100%)',
                info: 'linear-gradient(135deg, #74b9ff 0%, #0984e3 100%)',
                warning: 'linear-gradient(135deg, #fdcb6e 0%, #e17055 100%)',
                danger: 'linear-gradient(135deg, #fd79a8 0%, #e84393 100%)'
            }
        },
        text: {
            primary: '#ffffff',
            secondary: '#adb5bd',
            muted: '#6c757d',
            white: '#212529',
            light: '#495057'
        },
        borders: {
            color: '#495057',
            width: '1px',
            radius: '0.375rem'
        },
        shadows: {
            sm: '0 0.125rem 0.25rem rgba(255, 255, 255, 0.075)',
            md: '0 0.5rem 1rem rgba(255, 255, 255, 0.15)',
            lg: '0 1rem 3rem rgba(255, 255, 255, 0.175)',
            none: 'none'
        }
    },

    /**
     * 蓝色主题
     */
    blue: {
        name: 'blue',
        displayName: '蓝色主题',
        colors: {
            primary: '#2563eb',
            secondary: '#64748b',
            success: '#059669',
            info: '#0891b2',
            warning: '#d97706',
            danger: '#dc2626',
            light: '#f1f5f9',
            dark: '#1e293b',
            white: '#ffffff',
            black: '#000000'
        },
        backgrounds: {
            primary: '#2563eb',
            secondary: '#64748b',
            success: '#059669',
            info: '#0891b2',
            warning: '#d97706',
            danger: '#dc2626',
            light: '#f1f5f9',
            dark: '#1e293b',
            gradient: {
                primary: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
                success: 'linear-gradient(135deg, #10b981 0%, #047857 100%)',
                info: 'linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)',
                warning: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                danger: 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)'
            }
        },
        text: {
            primary: '#1e293b',
            secondary: '#64748b',
            muted: '#94a3b8',
            white: '#ffffff',
            light: '#cbd5e1'
        },
        borders: {
            color: '#e2e8f0',
            width: '1px',
            radius: '0.5rem'
        },
        shadows: {
            sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
            md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
            lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
            none: 'none'
        }
    },

    /**
     * 绿色主题
     */
    green: {
        name: 'green',
        displayName: '绿色主题',
        colors: {
            primary: '#059669',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#f0fdf4',
            dark: '#1f2937',
            white: '#ffffff',
            black: '#000000'
        },
        backgrounds: {
            primary: '#059669',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#f0fdf4',
            dark: '#1f2937',
            gradient: {
                primary: 'linear-gradient(135deg, #10b981 0%, #047857 100%)',
                success: 'linear-gradient(135deg, #34d399 0%, #10b981 100%)',
                info: 'linear-gradient(135deg, #22d3ee 0%, #0891b2 100%)',
                warning: 'linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%)',
                danger: 'linear-gradient(135deg, #f87171 0%, #ef4444 100%)'
            }
        },
        text: {
            primary: '#111827',
            secondary: '#6b7280',
            muted: '#9ca3af',
            white: '#ffffff',
            light: '#d1d5db'
        },
        borders: {
            color: '#d1fae5',
            width: '1px',
            radius: '0.5rem'
        },
        shadows: {
            sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
            md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
            lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
            none: 'none'
        }
    },

    /**
     * 紫色主题
     */
    purple: {
        name: 'purple',
        displayName: '紫色主题',
        colors: {
            primary: '#7c3aed',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#faf5ff',
            dark: '#1f2937',
            white: '#ffffff',
            black: '#000000'
        },
        backgrounds: {
            primary: '#7c3aed',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#faf5ff',
            dark: '#1f2937',
            gradient: {
                primary: 'linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)',
                success: 'linear-gradient(135deg, #34d399 0%, #10b981 100%)',
                info: 'linear-gradient(135deg, #22d3ee 0%, #0891b2 100%)',
                warning: 'linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%)',
                danger: 'linear-gradient(135deg, #f87171 0%, #ef4444 100%)'
            }
        },
        text: {
            primary: '#111827',
            secondary: '#6b7280',
            muted: '#9ca3af',
            white: '#ffffff',
            light: '#d1d5db'
        },
        borders: {
            color: '#ede9fe',
            width: '1px',
            radius: '0.5rem'
        },
        shadows: {
            sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
            md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
            lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
            none: 'none'
        }
    },

    /**
     * 橙色主题
     */
    orange: {
        name: 'orange',
        displayName: '橙色主题',
        colors: {
            primary: '#ea580c',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#fff7ed',
            dark: '#1f2937',
            white: '#ffffff',
            black: '#000000'
        },
        backgrounds: {
            primary: '#ea580c',
            secondary: '#6b7280',
            success: '#10b981',
            info: '#0891b2',
            warning: '#f59e0b',
            danger: '#ef4444',
            light: '#fff7ed',
            dark: '#1f2937',
            gradient: {
                primary: 'linear-gradient(135deg, #f97316 0%, #ea580c 100%)',
                success: 'linear-gradient(135deg, #34d399 0%, #10b981 100%)',
                info: 'linear-gradient(135deg, #22d3ee 0%, #0891b2 100%)',
                warning: 'linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%)',
                danger: 'linear-gradient(135deg, #f87171 0%, #ef4444 100%)'
            }
        },
        text: {
            primary: '#111827',
            secondary: '#6b7280',
            muted: '#9ca3af',
            white: '#ffffff',
            light: '#d1d5db'
        },
        borders: {
            color: '#fed7aa',
            width: '1px',
            radius: '0.5rem'
        },
        shadows: {
            sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
            md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
            lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
            none: 'none'
        }
    }
};

/**
 * 主题工具函数
 */
const ThemeUtils = {
    /**
     * 获取主题配置
     * @param {string} themeName - 主题名称
     * @returns {Object} 主题配置
     */
    getTheme(themeName) {
        return ThemeConfigs[themeName] || ThemeConfigs.default;
    },

    /**
     * 获取所有主题列表
     * @returns {Array} 主题列表
     */
    getAllThemes() {
        return Object.keys(ThemeConfigs).map(key => ({
            value: key,
            label: ThemeConfigs[key].displayName,
            theme: ThemeConfigs[key]
        }));
    },

    /**
     * 应用主题
     * @param {string} themeName - 主题名称
     * @param {Element} container - 容器元素
     */
    applyTheme(themeName, container) {
        const theme = this.getTheme(themeName);
        const root = container || document.documentElement;
        
        // 添加主题类名到根元素和body
        const elements = [root, document.body];
        elements.forEach(element => {
            if (element) {
                element.classList.remove(...Object.keys(ThemeConfigs).map(t => `amis-cards-theme-${t}`));
                element.classList.add(`amis-cards-theme-${themeName}`);
            }
        });
        
        console.log(`[AmisCards] 主题已应用: ${theme.displayName}`);
    },

    /**
     * 生成主题CSS
     * @param {string} themeName - 主题名称
     * @returns {string} CSS样式字符串
     */
    generateThemeCSS(themeName) {
        const theme = this.getTheme(themeName);
        const selector = `.amis-cards-theme-${themeName}`;
        
        let css = `${selector} {\n`;
        
        // 颜色变量
        Object.entries(theme.colors).forEach(([key, value]) => {
            css += `  --amis-cards-color-${key}: ${value};\n`;
        });
        
        // 文本颜色变量
        Object.entries(theme.text).forEach(([key, value]) => {
            css += `  --amis-cards-text-${key}: ${value};\n`;
        });
        
        // 背景变量
        Object.entries(theme.backgrounds).forEach(([key, value]) => {
            if (typeof value === 'string') {
                css += `  --amis-cards-bg-${key}: ${value};\n`;
            }
        });
        
        // 渐变背景变量
        Object.entries(theme.backgrounds.gradient).forEach(([key, value]) => {
            css += `  --amis-cards-gradient-${key}: ${value};\n`;
        });
        
        // 阴影变量
        Object.entries(theme.shadows).forEach(([key, value]) => {
            css += `  --amis-cards-shadow-${key}: ${value};\n`;
        });
        
        // 边框变量
        css += `  --amis-cards-border-color: ${theme.borders.color};\n`;
        css += `  --amis-cards-border-width: ${theme.borders.width};\n`;
        css += `  --amis-cards-border-radius: ${theme.borders.radius};\n`;
        
        css += '}\n';
        
        return css;
    },

    /**
     * 生成所有主题CSS
     * @returns {string} 完整的主题CSS
     */
    generateAllThemesCSS() {
        return Object.keys(ThemeConfigs)
            .map(themeName => this.generateThemeCSS(themeName))
            .join('\n');
    },

    /**
     * 切换主题
     * @param {string} fromTheme - 原主题
     * @param {string} toTheme - 目标主题
     * @param {Element} container - 容器元素
     * @param {Object} options - 选项
     */
    switchTheme(fromTheme, toTheme, container, options = {}) {
        const { duration = 300, easing = 'ease' } = options;
        const root = container || document.documentElement;
        
        // 添加过渡效果
        root.style.transition = `all ${duration}ms ${easing}`;
        
        // 应用新主题
        this.applyTheme(toTheme, root);
        
        // 移除过渡效果
        setTimeout(() => {
            root.style.transition = '';
        }, duration);
    }
};

// 注册到全局命名空间
window.AmisCards.ThemeConfigs = ThemeConfigs;
window.AmisCards.ThemeUtils = ThemeUtils;

console.log('[AmisCards] ThemeConfigs 已加载，共', Object.keys(ThemeConfigs).length, '个主题'); 