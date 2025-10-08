using CodeSpirit.ExamApi.Dtos.Question;
using System.Text;

namespace CodeSpirit.ExamApi.Services.Helpers;

/// <summary>
/// 默认提示词构建器实现
/// </summary>
public class DefaultPromptBuilder : IPromptBuilder
{
    private readonly ILogger<DefaultPromptBuilder> _logger;

    /// <summary>
    /// 初始化默认提示词构建器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultPromptBuilder(ILogger<DefaultPromptBuilder> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string BuildGenerationPrompt(AIGenerateQuestionDto request)
    {
        _logger.LogDebug("构建生成题目的提示词");
        
        var difficultyText = request.Difficulty switch
        {
            Data.Models.Enums.QuestionDifficulty.Easy => "简单",
            Data.Models.Enums.QuestionDifficulty.Medium => "中等",
            Data.Models.Enums.QuestionDifficulty.Hard => "困难",
            _ => "中等"
        };

        var typeText = request.Type switch
        {
            Data.Models.Enums.QuestionType.SingleChoice => "单选题",
            Data.Models.Enums.QuestionType.MultipleChoice => "多选题",
            Data.Models.Enums.QuestionType.TrueFalse => "判断题",
            _ => "单选题"
        };

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"请生成{request.Count}道关于\"{request.Topic}\"的{difficultyText}难度的{typeText}。");
        
        // 根据题型添加要求
        switch (request.Type)
        {
            case Data.Models.Enums.QuestionType.SingleChoice:
                sb.AppendLine("每道单选题必须包含4个选项，且只有一个正确答案。");
                sb.AppendLine("请严格按照要求，确保选项为数组格式且数量必须为4个，不得多也不得少。");
                sb.AppendLine("选项内容不应包含任何字母或数字序号（如A、B、C、D或1、2、3、4等），只需提供纯选项内容。");
                break;
            case Data.Models.Enums.QuestionType.MultipleChoice:
                sb.AppendLine("每道多选题必须包含4-6个选项，至少有2个正确答案。");
                sb.AppendLine("选项内容不应包含任何字母或数字序号（如A、B、C、D或1、2、3、4等），只需提供纯选项内容。");
                break;
            case Data.Models.Enums.QuestionType.TrueFalse:
                sb.AppendLine("每道判断题的答案必须是True或False。");
                break;
        }

        // 添加附加要求
        if (!string.IsNullOrEmpty(request.Requirements))
        {
            sb.AppendLine($"特别要求：{request.Requirements}");
        }

        sb.AppendLine("请以JSON格式输出，确保包含以下字段：");
        sb.AppendLine("1. content: 题目内容");
        sb.AppendLine("2. options: 选项数组（判断题不需要），选项内容不包含序号");
        sb.AppendLine("3. correctAnswer: 正确答案（单选题为完整选项文本，多选题为多个选项文本用逗号分隔，判断题为True或False）");
        sb.AppendLine("4. analysis: 答案解析");
        sb.AppendLine("5. knowledgePoints: 涉及的知识点");

        sb.AppendLine("示例输出格式:");
        sb.AppendLine("{\"questions\": [{\"content\": \"题目内容\",\"options\": [\"第一个选项内容\", \"第二个选项内容\", \"第三个选项内容\", \"第四个选项内容\"],\"correctAnswer\": \"第一个选项内容\",\"analysis\": \"答案解析\",\"knowledgePoints\": \"涉及的知识点\"}]}");

        var prompt = sb.ToString();
        
        // 在日志中打印完整的提示词
        _logger.LogInformation("=== AI题目生成提示词 ===");
        _logger.LogInformation("主题: {Topic}, 数量: {Count}, 类型: {Type}, 难度: {Difficulty}", 
            request.Topic, request.Count, typeText, difficultyText);
        _logger.LogInformation("完整提示词内容:\n{Prompt}", prompt);
        _logger.LogInformation("=== 提示词结束 ===");
        
        return prompt;
    }

    /// <inheritdoc/>
    public string BuildCorrectionPrompt(AIGenerateQuestionDto request, string? errorMessage = null)
    {
        _logger.LogDebug("构建格式修正的提示词");
        
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("你之前生成的回答存在格式问题，请修正。");
        
        // 如果有具体错误信息，将其包含在提示词中
        if (!string.IsNullOrEmpty(errorMessage))
        {
            sb.AppendLine();
            sb.AppendLine("具体错误信息：");
            sb.AppendLine(errorMessage);
        }
        
        sb.AppendLine();
        sb.AppendLine("我需要严格的JSON格式响应，格式如下:");
        sb.AppendLine("{\"questions\": [{\"content\": \"题目内容\",\"options\": [\"第一个选项内容\", \"第二个选项内容\", \"第三个选项内容\", \"第四个选项内容\"],\"correctAnswer\": \"第一个选项内容\",\"analysis\": \"答案解析\",\"knowledgePoints\": \"涉及的知识点\"}]}");
        sb.AppendLine();
        sb.AppendLine("特别要求：");
        sb.AppendLine("1. 响应必须是有效的JSON格式");
        sb.AppendLine("2. questions必须是数组");
        
        if (request.Type == Data.Models.Enums.QuestionType.SingleChoice)
        {
            sb.AppendLine("3. 对于单选题，options必须是包含4个选项的数组，不得多也不得少");
            sb.AppendLine("4. 选项内容不应包含任何字母或数字序号（如A、B、C、D或1、2、3、4等）");
            sb.AppendLine("5. correctAnswer必须是完整的选项文本内容，而不是选项序号");
        }
        else if (request.Type == Data.Models.Enums.QuestionType.MultipleChoice)
        {
            sb.AppendLine("3. 对于多选题，options必须是包含4-6个选项的数组");
            sb.AppendLine("4. 选项内容不应包含任何字母或数字序号（如A、B、C、D或1、2、3、4等）");
            sb.AppendLine("5. correctAnswer必须是用逗号分隔的完整选项文本列表");
        }
        else if (request.Type == Data.Models.Enums.QuestionType.TrueFalse)
        {
            sb.AppendLine("3. 对于判断题，correctAnswer必须是True或False");
        }
        
        sb.AppendLine();
        sb.AppendLine("你需要根据以下原始题目需求重新生成：");
        sb.AppendLine(BuildGenerationPrompt(request));
        sb.AppendLine();
        sb.AppendLine("请重新生成正确格式的JSON响应，不要包含任何额外文本、代码块标记或解释，只返回纯JSON。");
        
        var correctionPrompt = sb.ToString();
        
        // 在日志中打印格式修正提示词
        _logger.LogWarning("=== AI题目格式修正提示词 ===");
        _logger.LogWarning("错误信息: {ErrorMessage}", errorMessage ?? "无具体错误信息");
        _logger.LogWarning("修正提示词内容:\n{CorrectionPrompt}", correctionPrompt);
        _logger.LogWarning("=== 修正提示词结束 ===");
        
        return correctionPrompt;
    }
} 