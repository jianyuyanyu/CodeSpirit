/**
 * Token管理器
 * 负责用户认证token的存储、获取和清除
 */
window.TokenManager = (function() {
    'use strict';
    
    const TOKEN_KEY = 'auth_token';
    const REFRESH_TOKEN_KEY = 'refresh_token';
    const USER_INFO_KEY = 'user_info';
    const TOKEN_EXPIRY_KEY = 'token_expiry';
    
    /**
     * 设置认证token
     * @param {string} token 访问token
     * @param {string} refreshToken 刷新token（可选）
     * @param {number} expiresIn 过期时间（秒）
     */
    function setToken(token, refreshToken = null, expiresIn = null) {
        if (!token) {
            console.warn('Token不能为空');
            return;
        }
        
        try {
            localStorage.setItem(TOKEN_KEY, token);
            
            if (refreshToken) {
                localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
            }
            
            if (expiresIn) {
                const expiry = Date.now() + (expiresIn * 1000);
                localStorage.setItem(TOKEN_EXPIRY_KEY, expiry.toString());
            }
            
            console.log('Token已保存');
        } catch (error) {
            console.error('保存Token失败:', error);
        }
    }
    
    /**
     * 获取认证token
     * @returns {string|null} 访问token
     */
    function getToken() {
        try {
            const token = localStorage.getItem(TOKEN_KEY);
            
            // 检查token是否过期
            if (token && isTokenExpired()) {
                console.warn('Token已过期');
                clearToken();
                return null;
            }
            
            return token;
        } catch (error) {
            console.error('获取Token失败:', error);
            return null;
        }
    }
    
    /**
     * 获取刷新token
     * @returns {string|null} 刷新token
     */
    function getRefreshToken() {
        try {
            return localStorage.getItem(REFRESH_TOKEN_KEY);
        } catch (error) {
            console.error('获取刷新Token失败:', error);
            return null;
        }
    }
    
    /**
     * 检查token是否已过期
     * @returns {boolean} 是否过期
     */
    function isTokenExpired() {
        try {
            const expiry = localStorage.getItem(TOKEN_EXPIRY_KEY);
            if (!expiry) {
                return false; // 没有过期时间信息，假设未过期
            }
            
            return Date.now() > parseInt(expiry);
        } catch (error) {
            console.error('检查Token过期状态失败:', error);
            return false;
        }
    }
    
    /**
     * 清除所有认证信息
     */
    function clearToken() {
        try {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(REFRESH_TOKEN_KEY);
            localStorage.removeItem(USER_INFO_KEY);
            localStorage.removeItem(TOKEN_EXPIRY_KEY);
            console.log('Token已清除');
        } catch (error) {
            console.error('清除Token失败:', error);
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
            localStorage.setItem(USER_INFO_KEY, JSON.stringify(userInfo));
            console.log('用户信息已保存');
        } catch (error) {
            console.error('保存用户信息失败:', error);
        }
    }
    
    /**
     * 获取用户信息
     * @returns {Object|null} 用户信息对象
     */
    function getUserInfo() {
        try {
            const userInfo = localStorage.getItem(USER_INFO_KEY);
            return userInfo ? JSON.parse(userInfo) : null;
        } catch (error) {
            console.error('获取用户信息失败:', error);
            return null;
        }
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
        
        return {
            'Authorization': `Bearer ${token}`
        };
    }
    
    /**
     * 刷新token（如果有刷新token的话）
     * @param {string} refreshUrl 刷新token的API地址
     * @returns {Promise<boolean>} 刷新是否成功
     */
    async function refreshToken(refreshUrl = '/identity/api/identity/auth/refresh') {
        const refreshToken = getRefreshToken();
        if (!refreshToken) {
            console.warn('没有刷新Token');
            return false;
        }
        
        try {
            const response = await fetch(refreshUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Forwarded-With': 'CodeSpirit'
                },
                body: JSON.stringify({
                    refreshToken: refreshToken
                })
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.status === 0 && result.data) {
                    setToken(
                        result.data.accessToken,
                        result.data.refreshToken,
                        result.data.expiresIn
                    );
                    return true;
                }
            }
            
            console.warn('刷新Token失败');
            clearToken();
            return false;
        } catch (error) {
            console.error('刷新Token出错:', error);
            clearToken();
            return false;
        }
    }
    
    // 自动刷新token（在token即将过期时）
    function startAutoRefresh() {
        setInterval(() => {
            const expiry = localStorage.getItem(TOKEN_EXPIRY_KEY);
            if (expiry) {
                const timeLeft = parseInt(expiry) - Date.now();
                // 在过期前5分钟自动刷新
                if (timeLeft > 0 && timeLeft < 5 * 60 * 1000) {
                    refreshToken().catch(error => {
                        console.error('自动刷新Token失败:', error);
                    });
                }
            }
        }, 60000); // 每分钟检查一次
    }
    
    // 暴露公共API
    return {
        setToken,
        getToken,
        getRefreshToken,
        isTokenExpired,
        clearToken,
        setUserInfo,
        getUserInfo,
        isAuthenticated,
        getAuthHeaders,
        refreshToken,
        startAutoRefresh
    };
})(); 