/**
 * 通知客户端
 * 提供与通知服务的连接和消息处理功能
 */
class NotificationClient {
    constructor(hubUrl) {
        this.hubUrl = hubUrl;
        this.connection = null;
        this.handlers = new Map();
        this.connectionPromise = null;
    }

    /**
     * 连接到通知服务
     * @returns {Promise} 连接Promise
     */
    async connect() {
        if (this.connectionPromise) {
            return this.connectionPromise;
        }

        this.connectionPromise = new Promise((resolve, reject) => {
            try {
                this.connection = new signalR.HubConnectionBuilder()
                    .withUrl(this.hubUrl)
                    .withAutomaticReconnect()
                    .build();

                // 注册通知处理
                this.connection.on("ReceiveNotification", (message) => {
                    this.handleNotification(message);
                });

                // 启动连接
                this.connection.start()
                    .then(() => {
                        console.log("通知连接已建立");
                        resolve(this.connection);
                    })
                    .catch(err => {
                        console.error("建立通知连接失败:", err);
                        reject(err);
                    });
            } catch (err) {
                reject(err);
            }
        });

        return this.connectionPromise;
    }

    /**
     * 加入主题
     * @param {string} topic 主题名称
     * @param {string} [sessionId] 会话ID
     * @returns {Promise} 加入主题的Promise
     */
    async joinTopic(topic, sessionId = null) {
        await this.ensureConnected();
        await this.connection.invoke("JoinTopic", topic, sessionId);
    }

    /**
     * 离开主题
     * @param {string} topic 主题名称
     * @param {string} [sessionId] 会话ID
     * @returns {Promise} 离开主题的Promise
     */
    async leaveTopic(topic, sessionId = null) {
        if (this.connection) {
            await this.connection.invoke("LeaveTopic", topic, sessionId);
        }
    }

    /**
     * 注册消息处理器
     * @param {string} topic 主题名称
     * @param {string} type 消息类型
     * @param {Function} handler 处理函数
     */
    on(topic, type, handler) {
        const key = `${topic}:${type}`;
        if (!this.handlers.has(key)) {
            this.handlers.set(key, []);
        }
        this.handlers.get(key).push(handler);
    }

    /**
     * 取消注册消息处理器
     * @param {string} topic 主题名称
     * @param {string} type 消息类型
     * @param {Function} handler 处理函数
     */
    off(topic, type, handler) {
        const key = `${topic}:${type}`;
        if (this.handlers.has(key)) {
            const handlers = this.handlers.get(key);
            const index = handlers.indexOf(handler);
            if (index !== -1) {
                handlers.splice(index, 1);
            }
        }
    }

    /**
     * 处理通知消息
     * @param {Object} message 通知消息
     * @private
     */
    handleNotification(message) {
        // 确保消息数据存在且有效
        if (!message || !message.topic || !message.type) {
            console.error("收到无效的通知消息:", message);
            return;
        }

        const key = `${message.topic}:${message.type}`;
        const handlers = this.handlers.get(key) || [];
        
        handlers.forEach(handler => {
            try {
                handler(message.data);
            } catch (err) {
                console.error("处理通知失败:", err);
            }
        });
    }

    /**
     * 确保连接已建立
     * @returns {Promise} 连接Promise
     * @private
     */
    async ensureConnected() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            await this.connect();
        }
    }

    /**
     * 断开连接
     * @returns {Promise} 断开连接的Promise
     */
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            this.connectionPromise = null;
        }
    }
}

// 导出单例实例
window.NotificationClient = NotificationClient; 