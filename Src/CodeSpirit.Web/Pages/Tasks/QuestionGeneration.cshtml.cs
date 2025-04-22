using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Tasks;

/// <summary>
/// AI题目生成页面模型
/// </summary>
public class QuestionGenerationModel : PageModel
{
    private readonly ILogger<QuestionGenerationModel> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public QuestionGenerationModel(ILogger<QuestionGenerationModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 页面初始化
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("访问AI题目生成页面");
    }
} 