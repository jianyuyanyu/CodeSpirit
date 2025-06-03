/**
 * 通用Token管理器
 * 支持系统平台和租户平台的认证token管理
 * 根据平台类型使用不同的存储key
 * 
 * @example 系统平台使用:
 * ```javascript
 * // 设置token，24小时过期
 * TokenManager.setToken('your-token-here', 24);
 * 
 * // 获取token
 * const token = TokenManager.getToken();
 * ```
 * 
 * @example 租户平台使用:
 * ```javascript
 * // 初始化为租户模式
 * TokenManager.initTenantMode('tenant-id');
 * 
 * // 设置完整的租户token信息
 * TokenManager.setTokenExtended(
 *     'access-token',
 *     'refresh-token', 
 *     3600, // 过期时间（秒）
 *     'tenant-id'
 * );
 * ```
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 * @compatibility 完全兼容 tokenManager.js v1.0
 */
window.TokenManager = (function() {
    'use strict';
    
    // 平台类型
    let platformType = 'system'; // 'system' 或 'tenant'
    let currentTenantId = null;
    
    // 获取存储key
    function getStorageKeys() {
        if (platformType === 'tenant') {
            return {
                TOKEN_KEY: 'tenant_auth_token',
                REFRESH_TOKEN_KEY: 'tenant_refresh_token',
                USER_INFO_KEY: 'tenant_user_info',
                TOKEN_EXPIRY_KEY: 'tenant_token_expiry',
                TENANT_INFO_KEY: 'tenant_info'
            };
        } else {
            return {
                TOKEN_KEY: 'token',
                REFRESH_TOKEN_KEY: 'refresh_token',
                USER_INFO_KEY: 'user_info',
                TOKEN_EXPIRY_KEY: 'token_expiry',
                TENANT_INFO_KEY: 'system_info'
            };
        }
    }
    
    /**
     * 初始化租户模式
     * @param {string} tenantId 租户ID
     */
    function initTenantMode(tenantId) {
        platformType = 'tenant';
        currentTenantId = tenantId;
        console.log(`TokenManager: 已切换到租户模式 (${tenantId})`);
    }
    
    /**
     * 初始化系统模式
     */
    function initSystemMode() {
        platformType = 'system';
        currentTenantId = null;
        console.log('TokenManager: 已切换到系统模式');
    }
    
    /**
     * 设置认证token（兼容模式）
     * @param {string} token 访问token
     * @param {number} expiryInHours 过期时间（小时）
     */
    function setToken(token, expiryInHours = 24) {
        if (!token || typeof token !== 'string') {
            throw new Error('Token must be a non-empty string');
        }

        try {
            const keys = getStorageKeys();
            
            // 设置 token
            localStorage.setItem(keys.TOKEN_KEY, token);
            
            // 设置过期时间（转换为毫秒）
            const expiryTime = new Date();
            expiryTime.setHours(expiryTime.getHours() + expiryInHours);
            localStorage.setItem(keys.TOKEN_EXPIRY_KEY, expiryTime.getTime().toString());
            
            console.log(`${platformType === 'tenant' ? '租户' : '系统'}Token已保存`);
        } catch (error) {
            console.error('Error saving token:', error);
            throw new Error('Failed to save token');
        }
    }
    
    /**
     * 设置认证token（扩展版本 - 支持刷新token等）
     * @param {string} token 访问token
     * @param {string} refreshToken 刷新token（可选）
     * @param {number} expiresIn 过期时间（秒）
     * @param {string} tenantId 租户ID（可选）
     */
    function setTokenExtended(token, refreshToken = null, expiresIn = null, tenantId = null) {
        if (!token) {
            console.warn('Token不能为空');
            return;
        }
        
        try {
            const keys = getStorageKeys();
            
            localStorage.setItem(keys.TOKEN_KEY, token);
            
            if (refreshToken) {
                localStorage.setItem(keys.REFRESH_TOKEN_KEY, refreshToken);
            }
            
            if (expiresIn) {
                const expiry = Date.now() + (expiresIn * 1000);
                localStorage.setItem(keys.TOKEN_EXPIRY_KEY, expiry.toString());
            }
            
            // 保存租户ID或系统信息
            if (tenantId || currentTenantId) {
                const infoData = platformType === 'tenant' 
                    ? { tenantId: tenantId || currentTenantId }
                    : { platformType: 'system' };
                localStorage.setItem(keys.TENANT_INFO_KEY, JSON.stringify(infoData));
            }
            
            console.log(`${platformType === 'tenant' ? '租户' : '系统'}Token已保存（扩展模式）`);
        } catch (error) {
            console.error(`保存${platformType === 'tenant' ? '租户' : '系统'}Token失败:`, error);
        }
    }
    
    /**
     * 获取认证token
     * @returns {string|null} 访问token
     */
    function getToken() {
        try {
            const keys = getStorageKeys();
            const token = localStorage.getItem(keys.TOKEN_KEY);
            
            // 检查token是否过期
            if (token && isTokenExpired()) {
                console.warn(`${platformType === 'tenant' ? '租户' : '系统'}Token已过期`);
                clearToken();
                return null;
            }
            
            return token;
        } catch (error) {
            console.error(`获取${platformType === 'tenant' ? '租户' : '系统'}Token失败:`, error);
            return null;
        }
    }
    
    /**
     * 获取刷新token
     * @returns {string|null} 刷新token
     */
    function getRefreshToken() {
        try {
            const keys = getStorageKeys();
            return localStorage.getItem(keys.REFRESH_TOKEN_KEY);
        } catch (error) {
            console.error(`获取${platformType === 'tenant' ? '租户' : '系统'}刷新Token失败:`, error);
            return null;
        }
    }
    
    /**
     * 检查token是否已过期
     * @returns {boolean} 是否过期
     */
    function isTokenExpired() {
        try {
            const keys = getStorageKeys();
            const expiry = localStorage.getItem(keys.TOKEN_EXPIRY_KEY);
            if (!expiry) {
                return false; // 没有过期时间信息，假设未过期
            }
            
            // 支持两种格式：时间戳（毫秒）和ISO字符串
            let expiryTime;
            if (expiry.includes('-') || expiry.includes('T')) {
                // ISO字符串格式
                expiryTime = new Date(expiry).getTime();
            } else {
                // 时间戳格式
                expiryTime = parseInt(expiry);
            }
            
            return Date.now() > expiryTime;
        } catch (error) {
            console.error(`检查${platformType === 'tenant' ? '租户' : '系统'}Token过期状态失败:`, error);
            return false;
        }
    }
    
    /**
     * 检查是否有有效token
     * @returns {boolean} 是否有有效token
     */
    function hasToken() {
        return getToken() !== null;
    }
    
    /**
     * 清除所有认证信息
     */
    function clearToken() {
        try {
            const keys = getStorageKeys();
            
            localStorage.removeItem(keys.TOKEN_KEY);
            localStorage.removeItem(keys.REFRESH_TOKEN_KEY);
            localStorage.removeItem(keys.USER_INFO_KEY);
            localStorage.removeItem(keys.TOKEN_EXPIRY_KEY);
            localStorage.removeItem(keys.TENANT_INFO_KEY);
            
            console.log(`${platformType === 'tenant' ? '租户' : '系统'}Token已清除`);
        } catch (error) {
            console.error(`清除${platformType === 'tenant' ? '租户' : '系统'}Token失败:`, error);
        }
    }
    
    /**
     * 刷新token的过期时间
     * @param {number} expiryInHours 过期时间（小时）
     */
    function refreshTokenExpiry(expiryInHours = 24) {
        const token = getToken();
        if (token) {
            setToken(token, expiryInHours);
        }
    }
    
    /**
     * 设置用户信息
     * @param {Object} userInfo 用户信息对象
     */
    function setUserInfo(userInfo) {
        if (!userInfo) {
            console.warn('用户信息不能为空');
            return;
        }
        
        try {
            const keys = getStorageKeys();
            localStorage.setItem(keys.USER_INFO_KEY, JSON.stringify(userInfo));
            console.log(`${platformType === 'tenant' ? '租户' : '系统'}用户信息已保存`);
        } catch (error) {
            console.error(`保存${platformType === 'tenant' ? '租户' : '系统'}用户信息失败:`, error);
        }
    }
    
    /**
     * 获取用户信息
     * @returns {Object|null} 用户信息对象
     */
    function getUserInfo() {
        try {
            const keys = getStorageKeys();
            const userInfo = localStorage.getItem(keys.USER_INFO_KEY);
            return userInfo ? JSON.parse(userInfo) : null;
        } catch (error) {
            console.error(`获取${platformType === 'tenant' ? '租户' : '系统'}用户信息失败:`, error);
            return null;
        }
    }
    
    /**
     * 获取平台信息（租户信息或系统信息）
     * @returns {Object|null} 平台信息对象
     */
    function getPlatformInfo() {
        try {
            const keys = getStorageKeys();
            const platformInfo = localStorage.getItem(keys.TENANT_INFO_KEY);
            return platformInfo ? JSON.parse(platformInfo) : null;
        } catch (error) {
            console.error('获取平台信息失败:', error);
            return null;
        }
    }
    
    /**
     * 获取租户信息（兼容方法）
     * @returns {Object|null} 租户信息对象
     */
    function getTenantInfo() {
        if (platformType === 'tenant') {
            return getPlatformInfo();
        }
        return null;
    }
    
    /**
     * 检查用户是否已登录
     * @returns {boolean} 是否已登录
     */
    function isAuthenticated() {
        const token = getToken();
        return !!(token && !isTokenExpired());
    }
    
    /**
     * 获取认证头信息
     * @returns {Object} 包含Authorization头的对象
     */
    function getAuthHeaders() {
        const token = getToken();
        if (!token) {
            return {};
        }
        
        const headers = {
            'Authorization': `Bearer ${token}`
        };
        
        // 如果是租户模式，添加租户ID头
        if (platformType === 'tenant' && currentTenantId) {
            headers['X-Tenant-Id'] = currentTenantId;
        }
        
        return headers;
    }
    
    /**
     * 刷新token
     * @param {string} refreshUrl 刷新token的API地址
     * @returns {Promise<boolean>} 刷新是否成功
     */
    async function refreshToken(refreshUrl) {
        const refreshTokenValue = getRefreshToken();
        if (!refreshTokenValue) {
            console.warn(`没有${platformType === 'tenant' ? '租户' : '系统'}刷新Token`);
            return false;
        }
        
        // 根据平台类型确定默认刷新URL
        if (!refreshUrl) {
            refreshUrl = platformType === 'tenant' 
                ? '/identity/api/identity/auth/tenant/refresh'
                : '/identity/api/identity/auth/refresh';
        }
        
        const platformInfo = getPlatformInfo();
        
        try {
            const headers = {
                'Content-Type': 'application/json',
                'X-Forwarded-With': 'CodeSpirit'
            };
            
            const body = {
                refreshToken: refreshTokenValue
            };
            
            // 如果是租户模式，添加租户相关信息
            if (platformType === 'tenant') {
                headers['X-Tenant-Id'] = platformInfo?.tenantId || currentTenantId || '';
                body.tenantId = platformInfo?.tenantId || currentTenantId;
            }
            
            const response = await fetch(refreshUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(body)
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.status === 0 && result.data) {
                    if (platformType === 'tenant') {
                        setTokenExtended(
                            result.data.accessToken,
                            result.data.refreshToken,
                            result.data.expiresIn,
                            platformInfo?.tenantId || currentTenantId
                        );
                    } else {
                        setToken(result.data.accessToken);
                        if (result.data.refreshToken) {
                            const keys = getStorageKeys();
                            localStorage.setItem(keys.REFRESH_TOKEN_KEY, result.data.refreshToken);
                        }
                    }
                    return true;
                }
            }
            
            console.warn(`刷新${platformType === 'tenant' ? '租户' : '系统'}Token失败`);
            clearToken();
            return false;
        } catch (error) {
            console.error(`刷新${platformType === 'tenant' ? '租户' : '系统'}Token出错:`, error);
            clearToken();
            return false;
        }
    }
    
    /**
     * 自动刷新token
     */
    function startAutoRefresh() {
        setInterval(() => {
            const keys = getStorageKeys();
            const expiry = localStorage.getItem(keys.TOKEN_EXPIRY_KEY);
            if (expiry) {
                let timeLeft;
                if (expiry.includes('-') || expiry.includes('T')) {
                    // ISO字符串格式
                    timeLeft = new Date(expiry).getTime() - Date.now();
                } else {
                    // 时间戳格式
                    timeLeft = parseInt(expiry) - Date.now();
                }
                
                // 在过期前5分钟自动刷新
                if (timeLeft > 0 && timeLeft < 5 * 60 * 1000) {
                    refreshToken().catch(error => {
                        console.error(`自动刷新${platformType === 'tenant' ? '租户' : '系统'}Token失败:`, error);
                    });
                }
            }
        }, 60000); // 每分钟检查一次
    }
    
    /**
     * 检查是否存在其他平台的Token
     * @returns {boolean} 是否存在其他平台Token
     */
    function hasOtherPlatformToken() {
        if (platformType === 'tenant') {
            return !!(localStorage.getItem('token') || localStorage.getItem('auth_token'));
        } else {
            return !!(localStorage.getItem('tenant_auth_token'));
        }
    }
    
    /**
     * 清除其他平台Token
     */
    function clearOtherPlatformToken() {
        try {
            if (platformType === 'tenant') {
                // 清除系统平台Token
                localStorage.removeItem('token');
                localStorage.removeItem('auth_token');
                localStorage.removeItem('refresh_token');
                localStorage.removeItem('user_info');
                localStorage.removeItem('token_expiry');
                localStorage.removeItem('system_info');
                console.log('系统平台Token已清除');
            } else {
                // 清除租户平台Token
                localStorage.removeItem('tenant_auth_token');
                localStorage.removeItem('tenant_refresh_token');
                localStorage.removeItem('tenant_user_info');
                localStorage.removeItem('tenant_token_expiry');
                localStorage.removeItem('tenant_info');
                console.log('租户平台Token已清除');
            }
        } catch (error) {
            console.error('清除其他平台Token失败:', error);
        }
    }
    
    // 创建兼容对象，包含所有API
    const TokenManager = {
        // ===== 平台模式控制 =====
        initTenantMode,
        initSystemMode,
        
        // ===== 兼容 tokenManager.js 的属性 =====
        get TOKEN_KEY() { return getStorageKeys().TOKEN_KEY; },
        get TOKEN_EXPIRY_KEY() { return getStorageKeys().TOKEN_EXPIRY_KEY; },
        
        // ===== 兼容 tokenManager.js 的方法 =====
        setToken,
        getToken,
        clearToken,
        hasToken,
        isTokenExpired,
        refreshTokenExpiry,
        
        // ===== 扩展方法 =====
        setTokenExtended,
        getRefreshToken,
        setUserInfo,
        getUserInfo,
        getPlatformInfo,
        getTenantInfo, // 兼容方法
        isAuthenticated,
        getAuthHeaders,
        refreshToken,
        startAutoRefresh,
        hasOtherPlatformToken,
        clearOtherPlatformToken,
        
        // ===== 工具方法 =====
        get platformType() { return platformType; },
        get currentTenantId() { return currentTenantId; }
    };
    
    // ===== 模块化导出支持 =====
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = TokenManager;
    } else if (typeof define === 'function' && define.amd) {
        define([], function() {
            return TokenManager;
        });
    }
    
    return TokenManager;
})(); 