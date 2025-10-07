using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;
using Newtonsoft.Json.Linq;
using CodeSpirit.ExamApi.Controllers;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Core;
using CodeSpirit.Shared.Services;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Tests.Controllers
{
    public class QuestionsControllerTests
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<ILogger<QuestionsController>> _mockLogger;
        private readonly Mock<QuestionAiGeneratorService> _mockQuestionAiGeneratorService;
        private readonly Mock<IAiTaskService> _mockAiTaskService;
        private readonly QuestionsController _controller;

        public QuestionsControllerTests()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockLogger = new Mock<ILogger<QuestionsController>>();
            _mockQuestionAiGeneratorService = new Mock<QuestionAiGeneratorService>();
            _mockAiTaskService = new Mock<IAiTaskService>();
            
            _controller = new QuestionsController(
                _mockQuestionService.Object,
                _mockLogger.Object,
                _mockQuestionAiGeneratorService.Object,
                _mockAiTaskService.Object);
        }

        [Fact]
        public async Task GetQuestionPreviewConfig_WithSpecialCharacters_ReturnsCorrectAmisConfig()
        {
            // Arrange
            var questionId = 1L;
            var questionDto = new QuestionDto
            {
                Id = questionId,
                Content = "在JavaScript中，变量$userName和<div>标签的使用方法？",
                Type = QuestionType.SingleChoice,
                Options = new List<string>
                {
                    "var $userName = \"admin\";",
                    "<div>内容</div>",
                    "function test() { return true; }",
                    "console.log('Hello & World');"
                },
                CorrectAnswer = "var $userName = \"admin\";",
                Analysis = "JavaScript中$字符可以用于变量名，<>字符用于HTML标签。",
                DefaultScore = 2
            };

            _mockQuestionService.Setup(x => x.GetQuestionAsync(questionId))
                .ReturnsAsync(questionDto);

            // Act
            var result = await _controller.GetQuestionPreviewConfig(questionId);

            // Assert
            var response = Assert.IsType<ActionResult<ApiResponse<JObject>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var apiResponse = Assert.IsType<ApiResponse<JObject>>(okResult.Value);
            
            Assert.Equal(0, apiResponse.Status);
            Assert.NotNull(apiResponse.Data);
            
            var amisConfig = apiResponse.Data;
            Assert.Equal("form", amisConfig["type"]?.ToString());
            
            var body = amisConfig["body"] as JArray;
            Assert.NotNull(body);
            Assert.True(body.Count > 0);
            
            // 验证题目标题使用了raw过滤器
            var titleItem = body[0] as JObject;
            Assert.Equal("tpl", titleItem?["type"]?.ToString());
            var titleTpl = titleItem?["tpl"]?.ToString();
            Assert.Contains("${content | raw}", titleTpl);
            
            // 验证题目内容数据中$字符被正确编码
            var titleData = titleItem?["data"] as JObject;
            Assert.Equal("在JavaScript中，变量&#36;userName和<div>标签的使用方法？", titleData?["content"]?.ToString());
            
            // 验证选项配置包含labelTpl
            var optionsItem = body[1] as JObject;
            Assert.Equal("radios", optionsItem?["type"]?.ToString());
            Assert.Equal("${label | raw}", optionsItem?["labelTpl"]?.ToString());
            
            // 验证选项中的$字符被正确编码
            var options = optionsItem?["options"] as JArray;
            Assert.NotNull(options);
            Assert.Equal("var &#36;userName = \"admin\";", options[0]?["label"]?.ToString());
            
            // 验证正确答案使用了raw过滤器，且$字符被正确编码
            var answerItem = body.FirstOrDefault(item => 
                item is JObject obj && 
                obj["tpl"]?.ToString().Contains("正确答案") == true) as JObject;
            Assert.NotNull(answerItem);
            Assert.Contains("${answer | raw}", answerItem["tpl"]?.ToString());
            var answerData = answerItem["data"] as JObject;
            Assert.Equal("var &#36;userName = \"admin\";", answerData?["answer"]?.ToString());
            
            // 验证解析使用了raw过滤器，且$字符被正确编码
            var analysisItem = body.FirstOrDefault(item => 
                item is JObject obj && 
                obj["tpl"]?.ToString().Contains("解析") == true) as JObject;
            Assert.NotNull(analysisItem);
            Assert.Contains("${analysis | raw}", analysisItem["tpl"]?.ToString());
            var analysisData = analysisItem["data"] as JObject;
            Assert.Equal("JavaScript中&#36;字符可以用于变量名，<>字符用于HTML标签。", analysisData?["analysis"]?.ToString());
        }
    }
} 