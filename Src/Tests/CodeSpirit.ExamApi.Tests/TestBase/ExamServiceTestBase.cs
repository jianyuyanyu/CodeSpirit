using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeSpirit.ExamApi.Tests.TestBase
{
    /// <summary>
    /// ExamApi服务测试基类
    /// </summary>
    public abstract class ExamServiceTestBase : IDisposable
    {
        protected readonly string DatabaseName;
        protected ExamDbContext DbContext;
        protected ServiceProvider ServiceProvider;
        
        // 常用的Mock对象
        protected Mock<ILogger<QuestionService>> MockQuestionServiceLogger;
        protected IMapper Mapper;
        protected Mock<IDataFilter> MockDataFilter;
        protected Mock<ICurrentUser> MockCurrentUser;
        
        // 数据仓库
        protected Repository<Question> QuestionRepository;
        protected Repository<QuestionCategory> CategoryRepository;
        protected Repository<QuestionVersion> VersionRepository;
        
        protected ExamServiceTestBase()
        {
            // 为每个测试创建唯一的数据库名称
            DatabaseName = $"InMemoryDb_{Guid.NewGuid()}";
            
            // 初始化Mocks
            InitializeMocks();
            
            // 设置内存数据库
            SetupInMemoryDatabase();
            
            // 初始化仓库
            InitializeRepositories();
        }
        
        /// <summary>
        /// 初始化常用的Mock对象
        /// </summary>
        protected virtual void InitializeMocks()
        {
            MockQuestionServiceLogger = new Mock<ILogger<QuestionService>>();
            MockDataFilter = new Mock<IDataFilter>();
            MockCurrentUser = new Mock<ICurrentUser>();
            
            // 配置CurrentUser以返回当前用户信息
            MockCurrentUser.Setup(x => x.Id).Returns(1);
            MockCurrentUser.Setup(x => x.UserName).Returns("TestUser");
            MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        }
        
        /// <summary>
        /// 设置内存数据库
        /// </summary>
        protected virtual void SetupInMemoryDatabase()
        {
            var services = new ServiceCollection();
            
            // 注册DbContext
            services.AddDbContext<ExamDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            
            // 添加AutoMapper配置
            services.AddAutoMapper(cfg => {
                // 注册来自 ExamApi 的所有 Profile
                cfg.AddMaps(typeof(CodeSpirit.ExamApi.MappingProfiles.ExamPaperProfile).Assembly);
            });
            
            // 注册必要的服务
            services.AddSingleton(MockDataFilter.Object);
            services.AddSingleton(MockCurrentUser.Object);
            
            // 添加日志工厂
            services.AddLogging(builder => builder.AddDebug());
            
            // 创建服务提供者
            ServiceProvider = services.BuildServiceProvider();
            
            // 创建DbContext
            DbContext = ServiceProvider.GetRequiredService<ExamDbContext>();
            
            // 获取Mapper
            Mapper = ServiceProvider.GetRequiredService<IMapper>();
        }
        
        /// <summary>
        /// 初始化仓库
        /// </summary>
        protected virtual void InitializeRepositories()
        {
            QuestionRepository = new Repository<Question>(DbContext);
            CategoryRepository = new Repository<QuestionCategory>(DbContext);
            VersionRepository = new Repository<QuestionVersion>(DbContext);
        }
        
        /// <summary>
        /// 添加测试数据
        /// </summary>
        protected virtual void SeedTestData()
        {
            // 在子类中实现具体的测试数据填充
        }
        
        /// <summary>
        /// 添加测试题目数据
        /// </summary>
        protected void SeedQuestions(params Question[] questions)
        {
            DbContext.Set<Question>().AddRange(questions);
            DbContext.SaveChanges();
        }
        
        /// <summary>
        /// 添加测试分类数据
        /// </summary>
        protected void SeedCategories(params QuestionCategory[] categories)
        {
            DbContext.Set<QuestionCategory>().AddRange(categories);
            DbContext.SaveChanges();
        }
        
        /// <summary>
        /// 清理数据库上下文，避免实体跟踪冲突
        /// </summary>
        protected void ClearDbContext()
        {
            if (DbContext != null)
            {
                DbContext.ChangeTracker.Clear();
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            ClearDbContext();
            DbContext?.Dispose();
            ServiceProvider?.Dispose();
        }
        
        /// <summary>
        /// 创建通用的IRepository<T>实例
        /// </summary>
        protected Repository<T> CreateRepository<T>() where T : class
        {
            return new Repository<T>(DbContext);
        }
        
        /// <summary>
        /// 创建IRepository<T>的Mock实例
        /// </summary>
        protected Mock<IRepository<T>> CreateMockRepository<T>() where T : class
        {
            return new Mock<IRepository<T>>();
        }
    }
} 