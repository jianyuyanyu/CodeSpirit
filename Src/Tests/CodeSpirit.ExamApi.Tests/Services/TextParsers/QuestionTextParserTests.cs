using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Services.TextParsers;

namespace CodeSpirit.ExamApi.Tests.Services.TextParsers
{
    public class QuestionTextParserTests
    {
        private readonly Mock<ILogger<QuestionTextParser>> _mockLogger;
        private readonly QuestionTextParser _parser;
        
        public QuestionTextParserTests()
        {
            _mockLogger = new Mock<ILogger<QuestionTextParser>>();
            _parser = new QuestionTextParser(_mockLogger.Object);
        }

        [Fact]
        public void Parse_EmptyText_ReturnsEmptyList()
        {
            // Arrange
            string text = "";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Empty(result);
        }
        
        [Fact]
        public void Parse_NullText_ReturnsEmptyList()
        {
            // Arrange
            string text = null;
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Empty(result);
        }
        
        [Fact]
        public void Parse_SingleChoiceQuestion_ReturnsParsedResults()
        {
            // Arrange
            string text = @"一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Single(result);
            var question = result[0];
            
            Assert.Equal(QuestionType.SingleChoice, question.Type);
            Assert.Equal("SSL协议最早是由提出的。", question.Content);
            Assert.Equal(4, question.Options.Count);
            Assert.Equal("Microsoft", question.Options[0]);
            Assert.Equal("Netscape", question.Options[1]);
            Assert.Equal("ISO", question.Options[2]);
            Assert.Equal("IBM", question.Options[3]);
            Assert.Equal("Netscape", question.CorrectAnswer);
            Assert.Equal(1, question.Score);
            if (question.Analysis != null)
            {
                Assert.Equal("SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。", question.Analysis);
            }
            Assert.NotNull(question.Tags);
            if (question.Tags != null)
            {
                Assert.Equal(2, question.Tags.Count);
                Assert.Contains("SSL", question.Tags);
                Assert.Contains("安全通信", question.Tags);
            }
        }
        
        [Fact]
        public void Parse_TrueFalseQuestion_ReturnsParsedResults()
        {
            // Arrange
            string text = @"二、判断题（每空1分，共计20分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（ 对 ）
【解析】平邮包裹确实有网上查询物流进程的服务。
【标签】平邮、商品";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Single(result);
            var question = result[0];
            
            Assert.Equal(QuestionType.TrueFalse, question.Type);
            Assert.Equal("平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。", question.Content);
            Assert.Equal(2, question.Options.Count);
            Assert.Equal("True", question.Options[0]);
            Assert.Equal("False", question.Options[1]);
            Assert.Equal("True", question.CorrectAnswer);
            Assert.Equal(1, question.Score);
            Assert.Equal("平邮包裹确实有网上查询物流进程的服务。", question.Analysis);
            Assert.Equal(2, question.Tags.Count);
            Assert.Contains("平邮", question.Tags);
            Assert.Contains("商品", question.Tags);
        }
        
        [Fact]
        public void Parse_SingleChoiceQuestionWithoutBrackets_ParsesCorrectly()
        {
            // Arrange
            string text = @"一、单选题
1. 以下选项中，不属于常见的网络攻击方式的是
A. SQL注入
B. 社会工程学
C. 量子计算
D. 跨站脚本攻击";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Single(result);
            var question = result.First();
            
            Assert.Equal(QuestionType.SingleChoice, question.Type);
            Assert.Equal("以下选项中，不属于常见的网络攻击方式的是", question.Content);
            Assert.Equal(4, question.Options.Count);
            Assert.Equal("SQL注入", question.Options[0]);
            Assert.Equal("社会工程学", question.Options[1]);
            Assert.Equal("量子计算", question.Options[2]);
            Assert.Equal("跨站脚本攻击", question.Options[3]);
        }
        
        [Fact]
        public void Parse_TrueFalseQuestionWithFalseAnswer_ParsesCorrectly()
        {
            // Arrange
            string text = @"二、判断题
1. HTTP协议默认是加密传输的。（错）
【解析】HTTP协议默认是明文传输的，HTTPS才是加密传输。";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Single(result);
            var question = result.First();
            
            Assert.Equal(QuestionType.TrueFalse, question.Type);
            Assert.Equal("HTTP协议默认是加密传输的。", question.Content);
            Assert.Equal("False", question.CorrectAnswer);
            Assert.Equal("HTTP协议默认是明文传输的，HTTPS才是加密传输。", question.Analysis);
        }
        
        [Fact]
        public void Parse_MultipleQuestionsWithSpecificScores_ParsesScoresCorrectly()
        {
            // Arrange
            string text = @"一、单项选择题（每题2分）
1、以下[3分]关于计算机网络的说法，正确的是(A)
A、TCP是网络层协议
B、UDP提供可靠传输
C、HTTP是应用层协议
D、IP地址属于数据链路层

2、操作系统的主要功能不包括(D)
A、内存管理
B、进程管理
C、设备管理
D、音频处理";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Equal(2, result.Count);
            
            var question1 = result[0];
            var question2 = result[1];
            
            Assert.Equal(3, question1.Score); // 从题目中提取的分数优先于标题默认分数
            Assert.Equal(2, question2.Score); // 使用标题中的默认分数
        }
        
        [Fact]
        public void Parse_CompleteExamText_ParsesAllQuestions()
        {
            // Arrange
            string text = @"一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信

2、以下不属于电子商务隐私权保护对策的是()。
A、提高消费者的隐私权保护意识
B、逐步完善消费者隐私权的科技保护手段
C、设立消费者隐私保护的法律
D、规范网络伦理的规约体系

二、判断题（每空1分，共计20分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（ 对 ）
【解析】平邮包裹确实有网上查询物流进程的服务。
【标签】平邮、商品

2. 目前网店大多数都是使用淘宝、易趣、拍拍等第三方平台开启，自己制作电子商务站点技术量较大，且前期投入巨大。（ 对 ）";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Equal(4, result.Count);
            
            // 验证单选题
            Assert.Equal(2, result.Count(q => q.Type == QuestionType.SingleChoice));
            // 验证判断题
            Assert.Equal(2, result.Count(q => q.Type == QuestionType.TrueFalse));
            
            // 检查第一个判断题
            var trueFalseQuestion = result.First(q => q.Type == QuestionType.TrueFalse);
            Assert.Equal("True", trueFalseQuestion.CorrectAnswer);
        }
        
        [Fact]
        public void Parse_QuestionWithSpecialFormats_HandlesDifferentFormatsCorrectly()
        {
            // Arrange
            string text = @"一、单选题
1. 下列（B）是开源操作系统
A. Windows
B. Linux
C. macOS
D. iOS

2. 以下哪项是TCP/IP协议栈中的应用层协议？ B 
A. IP
B. HTTP
C. TCP
D. UDP";
            
            // Act
            var result = _parser.Parse(text);
            
            // Assert
            Assert.Equal(2, result.Count);
            
            // 检查第一题 - (B) 格式
            var question1 = result[0];
            Assert.Equal("Linux", question1.CorrectAnswer);
            
            // 检查第二题 - B 格式
            var question2 = result[1];
            Assert.Equal("HTTP", question2.CorrectAnswer);
        }
    }
} 