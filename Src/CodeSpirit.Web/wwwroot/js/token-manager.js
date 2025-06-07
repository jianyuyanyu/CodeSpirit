/**
 * 通用Token管理器
 * 支持系统平台、租户平台和客户端平台的认证token管理
 * 根据平台类型使用不同的存储key
 * 客户端平台支持考试系统、培训系统、学习系统等多种类型
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
 * @example 客户端平台使用:
 * ```javascript
 * // 初始化为客户端模式
 * TokenManager.initClientMode('tenant-id', 'exam');
 * 
 * // 设置完整的客户端token信息
 * TokenManager.setTokenExtended(
 *     'access-token',
 *     'refresh-token', 
 *     3600,
 *     'tenant-id'
 * );
 * ```
 * 
 * @version 2.1.0
 * @author CodeSpirit Team
 * @compatibility 完全兼容 tokenManager.js v1.0
 */
window.TokenManager = (function() {
    'use strict';
    
    // 平台类型
    let platformType = 'system'; // 'system', 'tenant' 或 'client'
    let currentTenantId = null;
    let currentClientType = 'exam'; // 客户端类型：exam, training, learning, assessment等
    
    // 获取存储key
    function getStorageKeys() {
        if (platformType === 'client') {
            return {
                TOKEN_KEY: `client_${currentClientType}_auth_token`,
                REFRESH_TOKEN_KEY: `client_${currentClientType}_refresh_token`,
                USER_INFO_KEY: `client_${currentClientType}_user_info`,
                TOKEN_EXPIRY_KEY: `client_${currentClientType}_token_expiry`,
                TENANT_INFO_KEY: `client_${currentClientType}_tenant_info`
            };
        } else if (platformType === 'tenant') {
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
        currentClientType = null;
        console.log(`TokenManager: 已切换到租户模式 (${tenantId})`);
    }
    
    /**
     * 初始化系统模式
     */
    function initSystemMode() {
        platformType = 'system';
        currentTenantId = null;
        currentClientType = null;
        console.log('TokenManager: 已切换到系统模式');
    }
    
    /**
     * 初始化客户端模式
     * @param {string} tenantId 租户ID（可选）
     * @param {string} clientType 客户端类型 (exam, training, learning, assessment等)
     */
    function initClientMode(tenantId = null, clientType = 'exam') {
        platformType = 'client';
        currentTenantId = tenantId;
        currentClientType = clientType;
        console.log(`TokenManager: 已切换到客户端模式 ${clientType}${tenantId ? ` (租户: ${tenantId})` : ''}`);
    }
    
    /**
     * 设置客户端类型
     * @param {string} clientType 客户端类型
     */
    function setClientType(clientType) {
        if (platformType === 'client') {
            currentClientType = clientType;
            console.log(`TokenManager: 客户端类型已设置为 ${clientType}`);
        } else {
            console.warn('TokenManager: 仅在客户端模式下可以设置客户端类型');
        }
    }
    
    /**
     * 获取当前客户端类型显示名称
     * @returns {string} 客户端类型显示名称
     */
    function getClientTypeName() {
        switch (currentClientType?.toLowerCase()) {
            case 'exam': return '考试系统';
            case 'training': return '培训系统';
            case 'learning': return '学习系统';
            case 'assessment': return '评估系统';
            default: return '客户端系统';
        }
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
            
            const platformName = getPlatformName();
            console.log(`${platformName}Token已保存`);
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
                const infoData = platformType === 'system' 
                    ? { platformType: 'system' }
                    : platformType === 'client'
                    ? { tenantId: tenantId || currentTenantId, clientType: currentClientType }
                    : { tenantId: tenantId || currentTenantId };
                localStorage.setItem(keys.TENANT_INFO_KEY, JSON.stringify(infoData));
            }
            
            const platformName = getPlatformName();
            console.log(`${platformName}Token已保存（扩展模式）`);
        } catch (error) {
            const platformName = getPlatformName();
            console.error(`保存${platformName}Token失败:`, error);
        }
    }
    
    /**
     * 获取平台名称（用于日志显示）
     * @returns {string} 平台名称
     */
    function getPlatformName() {
        switch (platformType) {
            case 'tenant': return '租户';
            case 'client': return getClientTypeName();
            default: return '系统';
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
                const platformName = getPlatformName();
                console.warn(`${platformName}Token已过期`);
                clearToken();
                return null;
            }
            
            return token;
        } catch (error) {
            const platformName = getPlatformName();
            console.error(`获取${platformName}Token失败:`, error);
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
            const platformName = getPlatformName();
            console.error(`获取${platformName}刷新Token失败:`, error);
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
            const platformName = getPlatformName();
            console.error(`检查${platformName}Token过期状态失败:`, error);
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
            
            const platformName = getPlatformName();
            console.log(`${platformName}Token已清除`);
        } catch (error) {
            const platformName = getPlatformName();
            console.error(`清除${platformName}Token失败:`, error);
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
            const platformName = getPlatformName();
            console.log(`${platformName}用户信息已保存`);
        } catch (error) {
            const platformName = getPlatformName();
            console.error(`保存${platformName}用户信息失败:`, error);
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
            const platformName = getPlatformName();
            console.error(`获取${platformName}用户信息失败:`, error);
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
        if (platformType === 'tenant' || platformType === 'client') {
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
        
        // 如果是租户模式或客户端模式，添加租户ID头
        if ((platformType === 'tenant' || platformType === 'client') && currentTenantId) {
            headers['X-Tenant-Id'] = currentTenantId;
        }
        
        // 如果是客户端模式，添加客户端类型头
        if (platformType === 'client' && currentClientType) {
            headers['X-Client-Type'] = currentClientType;
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
            const platformName = getPlatformName();
            console.warn(`没有${platformName}刷新Token`);
            return false;
        }
        
        // 根据平台类型确定默认刷新URL
        if (!refreshUrl) {
            switch (platformType) {
                case 'client':
                    refreshUrl = '/identity/api/identity/auth/tenant/refresh'; // 客户端使用租户刷新接口
                    break;
                case 'tenant':
                    refreshUrl = '/identity/api/identity/auth/tenant/refresh';
                    break;
                default:
                    refreshUrl = '/identity/api/identity/auth/refresh';
                    break;
            }
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
            
            // 如果是租户模式或客户端模式，添加租户相关信息
            if (platformType === 'tenant' || platformType === 'client') {
                headers['X-Tenant-Id'] = platformInfo?.tenantId || currentTenantId || '';
                body.tenantId = platformInfo?.tenantId || currentTenantId;
                
                // 如果是客户端模式，添加客户端类型信息
                if (platformType === 'client') {
                    headers['X-Client-Type'] = currentClientType;
                    body.clientType = currentClientType;
                }
            }
            
            const response = await fetch(refreshUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(body)
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.status === 0 && result.data) {
                    if (platformType === 'tenant' || platformType === 'client') {
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
            
            const platformName = getPlatformName();
            console.warn(`刷新${platformName}Token失败`);
            clearToken();
            return false;
        } catch (error) {
            const platformName = getPlatformName();
            console.error(`刷新${platformName}Token出错:`, error);
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
                        const platformName = getPlatformName();
                        console.error(`自动刷新${platformName}Token失败:`, error);
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
        } else if (platformType === 'client') {
            // 检查是否存在系统或租户平台的token
            return !!(localStorage.getItem('token') || localStorage.getItem('tenant_auth_token'));
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
            } else if (platformType === 'client') {
                // 清除系统和租户平台Token
                localStorage.removeItem('token');
                localStorage.removeItem('auth_token');
                localStorage.removeItem('refresh_token');
                localStorage.removeItem('user_info');
                localStorage.removeItem('token_expiry');
                localStorage.removeItem('system_info');
                localStorage.removeItem('tenant_auth_token');
                localStorage.removeItem('tenant_refresh_token');
                localStorage.removeItem('tenant_user_info');
                localStorage.removeItem('tenant_token_expiry');
                localStorage.removeItem('tenant_info');
                console.log('系统和租户平台Token已清除');
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
        initClientMode,
        setClientType,
        
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
        get currentTenantId() { return currentTenantId; },
        get currentClientType() { return currentClientType; },
        getClientTypeName
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