using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.Shared.Repositories;
using Moq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.ExamApi.Tests.TestBase;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Tests.Services
{
    public class QuestionServiceTests : ExamServiceTestBase
    {
        private readonly QuestionService _questionService;
        private readonly Mock<IRepository<Question>> _mockQuestionRepository;
        private readonly Mock<IRepository<QuestionCategory>> _mockCategoryRepository;
        private readonly Mock<IRepository<QuestionVersion>> _mockVersionRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<QuestionService>> _mockLogger;
        
        public QuestionServiceTests()
        {
            _mockQuestionRepository = new Mock<IRepository<Question>>();
            _mockCategoryRepository = new Mock<IRepository<QuestionCategory>>();
            _mockVersionRepository = new Mock<IRepository<QuestionVersion>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<QuestionService>>();
            
            _questionService = new QuestionService(
                _mockQuestionRepository.Object,
                _mockCategoryRepository.Object,
                _mockVersionRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
            
            SetupMocks();
        }
        
        private void SetupMocks()
        {
            // 设置查询方法
            var mockQuery = new List<Question>().AsQueryable();
            _mockQuestionRepository.Setup(repo => repo.CreateQuery())
                .Returns(mockQuery);
                
            // 设置一个模拟查询结果
            var mockFilteredQuery = new List<Question>().AsQueryable();
            // 用于处理Where表达式的模拟
            _mockQuestionRepository.Setup(repo => repo.Find(It.IsAny<Expression<Func<Question, bool>>>()))
                .Returns(mockFilteredQuery);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_EmptyText_ThrowsException()
        {
            // Arrange
            var input = new QuestionImportFromTextDto
            {
                Text = "",
                CategoryId = 1,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppServiceException>(() => 
                _questionService.ImportFromTextAsync(input));
            
            Assert.Equal(400, exception.Code);
            Assert.Equal("试卷文本内容不能为空！", exception.Message);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_CategoryNotFound_ThrowsException()
        {
            // Arrange
            var input = new QuestionImportFromTextDto
            {
                Text = "测试文本",
                CategoryId = 999,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // 设置分类不存在
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(It.Is<long>(id => id == 999)))
                .ReturnsAsync((QuestionCategory)null);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppServiceException>(() => 
                _questionService.ImportFromTextAsync(input));
            
            Assert.Equal(400, exception.Code);
            Assert.Equal("所选分类不存在！", exception.Message);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_ValidSingleChoiceQuestion_ImportsSuccessfully()
        {
            // Arrange
            var questionContent = @"
一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由()提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM";
            
            var input = new QuestionImportFromTextDto
            {
                Text = questionContent,
                CategoryId = 1,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // 创建有效的分类对象
            var category = new QuestionCategory { Id = 1, Name = "测试分类" };
            
            // 设置分类存在
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(It.Is<long>(id => id == 1)))
                .ReturnsAsync(category);
            
            // 设置查询方法返回空集合，表示没有重复的题目
            var emptyQuery = new List<Question>().AsQueryable();
            _mockQuestionRepository.Setup(repo => repo.Find(It.IsAny<Expression<Func<Question, bool>>>()))
                .Returns(emptyQuery);
            
            // 设置添加方法
            _mockQuestionRepository.Setup(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()))
                .ReturnsAsync((Question q, bool _) => q);
            
            _mockQuestionRepository.Setup(repo => repo.SaveChangesAsync())
                .ReturnsAsync(1);
            
            // Act
            var result = await _questionService.ImportFromTextAsync(input);
            
            // Assert
            Assert.Equal(1, result.successCount);
            Assert.Empty(result.failedItems);
            
            // 验证仓库方法是否被调用
            _mockQuestionRepository.Verify(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()), Times.Once);
            _mockQuestionRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_ValidTrueFalseQuestion_ImportsSuccessfully()
        {
            // Arrange
            var questionContent = @"
二、判断题（每空1分，共计20分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（  ）";
            
            var input = new QuestionImportFromTextDto
            {
                Text = questionContent,
                CategoryId = 1,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // 创建有效的分类对象
            var category = new QuestionCategory { Id = 1, Name = "测试分类" };
            
            // 设置分类存在
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(It.Is<long>(id => id == 1)))
                .ReturnsAsync(category);
            
            // 设置查询方法返回空集合，表示没有重复的题目
            var emptyQuery = new List<Question>().AsQueryable();
            _mockQuestionRepository.Setup(repo => repo.Find(It.IsAny<Expression<Func<Question, bool>>>()))
                .Returns(emptyQuery);
            
            // 设置添加方法
            _mockQuestionRepository.Setup(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()))
                .ReturnsAsync((Question q, bool _) => q);
            
            _mockQuestionRepository.Setup(repo => repo.SaveChangesAsync())
                .ReturnsAsync(1);
            
            // Act
            var result = await _questionService.ImportFromTextAsync(input);
            
            // Assert
            Assert.Equal(1, result.successCount);
            Assert.Empty(result.failedItems);
            
            // 验证仓库方法是否被调用
            _mockQuestionRepository.Verify(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()), Times.Once);
            _mockQuestionRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_DuplicateQuestion_SkipsImport()
        {
            // Arrange
            var questionContent = @"
一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由()提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM";
            
            var input = new QuestionImportFromTextDto
            {
                Text = questionContent,
                CategoryId = 1,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // 创建有效的分类对象
            var category = new QuestionCategory { Id = 1, Name = "测试分类" };
            
            // 设置分类存在
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(It.Is<long>(id => id == 1)))
                .ReturnsAsync(category);
            
            // 设置问题已存在
            var existingQuestion = new Question
            {
                Id = 1,
                Content = "1. SSL协议最早是由()提出的。",
                Type = QuestionType.SingleChoice
            };
            
            var mockQueryable = new List<Question> { existingQuestion }.AsQueryable();
            
            // 使用Find而不是Where，因为Where是扩展方法
            _mockQuestionRepository.Setup(repo => repo.Find(It.IsAny<Expression<Func<Question, bool>>>()))
                .Returns(mockQueryable);
            
            // Act
            var result = await _questionService.ImportFromTextAsync(input);
            
            // Assert
            Assert.Equal(0, result.successCount);
            Assert.NotEmpty(result.failedItems);
            
            // 验证仓库方法是否被调用（应该未被调用，因为问题已存在）
            _mockQuestionRepository.Verify(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()), Times.Never);
        }
        
        [Fact]
        public async Task ImportFromTextAsync_CompleteExam_ImportsAllQuestions()
        {
            // Arrange
            var examContent = @"
一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由()提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
2、以下不属于电子商务隐私权保护对策的是()。
A、提高消费者的隐私权保护意识
B、逐步完善消费者隐私权的科技保护手段
C、设立消费者隐私保护的法律
D、规范网络伦理的规约体系

2、判断题（每空1分，共计20分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（  ）
2. 目前网店大多数都是使用淘宝、易趣、拍拍等第三方平台开启，自己制作电子商务站点技术量较大，且前期投入巨大。（  ）";
            
            var input = new QuestionImportFromTextDto
            {
                Text = examContent,
                CategoryId = 1,
                QuestionDifficulty = QuestionDifficulty.Medium
            };
            
            // 创建有效的分类对象
            var category = new QuestionCategory { Id = 1, Name = "测试分类" };
            
            // 设置分类存在
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(It.Is<long>(id => id == 1)))
                .ReturnsAsync(category);
            
            // 设置查询方法返回空集合，表示没有重复的题目
            var emptyQuery = new List<Question>().AsQueryable();
            _mockQuestionRepository.Setup(repo => repo.Find(It.IsAny<Expression<Func<Question, bool>>>()))
                .Returns(emptyQuery);
            
            // 设置添加方法
            _mockQuestionRepository.Setup(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()))
                .ReturnsAsync((Question q, bool _) => q);
            
            _mockQuestionRepository.Setup(repo => repo.SaveChangesAsync())
                .ReturnsAsync(4);
            
            // Act
            var result = await _questionService.ImportFromTextAsync(input);
            
            // Assert
            Assert.Equal(4, result.successCount); // 2个单选题 + 2个判断题
            Assert.Empty(result.failedItems);
            
            // 验证仓库方法是否被调用
            _mockQuestionRepository.Verify(repo => repo.AddAsync(It.IsAny<Question>(), It.IsAny<bool>()), Times.Exactly(4));
            _mockQuestionRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
    }
} 