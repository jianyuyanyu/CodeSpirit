namespace CodeSpirit.LLM.Prompts;

/// <summary>
/// LLM提示词构建器接口
/// </summary>
public interface ILLMPromptBuilder
{
    /// <summary>
    /// 设置系统提示词
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithSystemPrompt(string systemPrompt);

    /// <summary>
    /// 使用模板构建提示词
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="parameters">模板参数</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithTemplate(string templateName, object parameters);

    /// <summary>
    /// 添加验证规则
    /// </summary>
    /// <param name="rules">验证规则列表</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithValidationRules(params string[] rules);

    /// <summary>
    /// 添加示例
    /// </summary>
    /// <param name="examples">示例列表</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithExamples(params object[] examples);

    /// <summary>
    /// 指定输出格式
    /// </summary>
    /// <typeparam name="T">输出类型</typeparam>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithOutputFormat<T>();

    /// <summary>
    /// 添加自定义指令
    /// </summary>
    /// <param name="instruction">指令内容</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithInstruction(string instruction);

    /// <summary>
    /// 添加上下文信息
    /// </summary>
    /// <param name="context">上下文内容</param>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder WithContext(string context);

    /// <summary>
    /// 构建最终的提示词
    /// </summary>
    /// <returns>构建的提示词</returns>
    string Build();

    /// <summary>
    /// 重置构建器状态
    /// </summary>
    /// <returns>构建器实例</returns>
    ILLMPromptBuilder Reset();
}

/// <summary>
/// LLM提示词模板管理器接口
/// </summary>
public interface ILLMPromptTemplateManager
{
    /// <summary>
    /// 注册模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <param name="template">模板内容</param>
    void RegisterTemplate(string name, string template);

    /// <summary>
    /// 获取模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <returns>模板内容</returns>
    string GetTemplate(string name);

    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <param name="parameters">模板参数</param>
    /// <returns>渲染后的内容</returns>
    string RenderTemplate(string name, object parameters);

    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <returns>是否存在</returns>
    bool HasTemplate(string name);

    /// <summary>
    /// 获取所有模板名称
    /// </summary>
    /// <returns>模板名称列表</returns>
    IEnumerable<string> GetTemplateNames();
}
