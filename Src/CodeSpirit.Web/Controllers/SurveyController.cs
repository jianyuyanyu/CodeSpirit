//using CodeSpirit.Core.Attributes;
//using CodeSpirit.Core.Enums;
//using Microsoft.AspNetCore.Mvc;
//using System.ComponentModel;

//namespace CodeSpirit.Web.Controllers;

///// <summary>
///// 问卷调查控制器
///// </summary>
//[DisplayName("问卷调查")]
//[Navigation(Icon = "fa-solid fa-poll", Order = 100, PlatformType = PlatformType.System, Group = "应用")]
//public class SurveyController : ApiControllerBase
//{
//    private readonly ILogger<SurveyController> _logger;

//    /// <summary>
//    /// 初始化问卷调查控制器
//    /// </summary>
//    /// <param name="logger">日志记录器</param>
//    public SurveyController(ILogger<SurveyController> logger)
//    {
//        _logger = logger;
//    }

//    /// <summary>
//    /// 问卷列表页面
//    /// </summary>
//    /// <returns>问卷列表页面</returns>
//    [HttpGet]
//    [DisplayName("问卷列表")]
//    public IActionResult Index()
//    {
//        _logger.LogInformation("访问问卷列表页面");
//        return Redirect("/survey");
//    }

//    /// <summary>
//    /// 参与问卷页面
//    /// </summary>
//    /// <param name="id">问卷ID</param>
//    /// <returns>参与问卷页面</returns>
//    [HttpGet("participate/{id}")]
//    [DisplayName("参与问卷")]
//    public IActionResult Participate(int id)
//    {
//        _logger.LogInformation("访问参与问卷页面，问卷ID: {SurveyId}", id);
//        return Redirect($"/survey/participate/{id}");
//    }
//}
