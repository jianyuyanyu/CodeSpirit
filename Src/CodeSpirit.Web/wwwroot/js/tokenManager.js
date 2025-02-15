const TokenManager = {
    // Token 的 key 名称
    TOKEN_KEY: 'token',
    TOKEN_EXPIRY_KEY: 'token_expiry',

    // 保存 token
    setToken(token, expiryInHours = 24) {
        if (!token || typeof token !== 'string') {
            throw new Error('Token must be a non-empty string');
        }

        try {
            // 设置 token
            localStorage.setItem(this.TOKEN_KEY, token);
            
            // 设置过期时间
            const expiryTime = new Date();
            expiryTime.setHours(expiryTime.getHours() + expiryInHours);
            localStorage.setItem(this.TOKEN_EXPIRY_KEY, expiryTime.toISOString());
        } catch (error) {
            console.error('Error saving token:', error);
            throw new Error('Failed to save token');
        }
    },

    // 获取 token
    getToken() {
        try {
            const token = localStorage.getItem(this.TOKEN_KEY);
            const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);

            if (!token || !expiry) {
                return null;
            }

            // 检查是否过期
            if (new Date(expiry) < new Date()) {
                this.clearToken();
                return null;
            }

            return token;
        } catch (error) {
            console.error('Error retrieving token:', error);
            return null;
        }
    },

    // 清除 token
    clearToken() {
        try {
            localStorage.removeItem(this.TOKEN_KEY);
            localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
        } catch (error) {
            console.error('Error clearing token:', error);
        }
    },

    // 检查是否有 token
    hasToken() {
        return this.getToken() !== null;
    },

    isTokenExpired() {
        try {
            const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
            return !expiry || new Date(expiry) < new Date();
        } catch (error) {
            console.error('Error checking token expiry:', error);
            return true;
        }
    },

    // 刷新 token 的过期时间
    refreshTokenExpiry(expiryInHours = 24) {
        const token = this.getToken();
        if (token) {
            this.setToken(token, expiryInHours);
        }
    }
};

// 将 ES modules 的导出改为 CommonJS 风格
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TokenManager;
} else if (typeof define === 'function' && define.amd) {
    define([], function() {
        return TokenManager;
    });
} else {
    window.TokenManager = TokenManager;
} 