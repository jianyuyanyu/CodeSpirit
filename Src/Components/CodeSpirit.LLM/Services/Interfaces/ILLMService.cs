using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.LLM.Models;
using Microsoft.SemanticKernel;

namespace CodeSpirit.LLM.Services.Interfaces
{
    /// <summary>
    /// LLM服务接口
    /// </summary>
    public interface ILLMService : IScopedDependency
    {
        /// <summary>
        /// 聊天对话
        /// </summary>
        /// <param name="messages">消息列表</param>
        /// <param name="options">聊天选项</param>
        /// <returns>聊天响应</returns>
        Task<ChatResponse> ChatAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null);
        
        /// <summary>
        /// 简单对话（单轮）
        /// </summary>
        /// <param name="userMessage">用户消息</param>
        /// <param name="options">聊天选项</param>
        /// <returns>模型回复</returns>
        Task<string> AskAsync(string userMessage, ChatOptions options = null);
        
        /// <summary>
        /// 执行提示模板
        /// </summary>
        /// <param name="promptTemplate">提示模板</param>
        /// <param name="parameters">参数值字典</param>
        /// <param name="options">聊天选项</param>
        /// <returns>模型回复</returns>
        Task<string> ExecutePromptTemplateAsync(string promptTemplate, Dictionary<string, string> parameters, ChatOptions options = null);
        
        /// <summary>
        /// 生成文本嵌入
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>嵌入向量</returns>
        Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text);
        
        /// <summary>
        /// 获取底层Kernel实例
        /// </summary>
        /// <returns>Semantic Kernel实例</returns>
        Kernel GetKernel();
    }
} 