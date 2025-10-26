using CodeSpirit.Core;
using CodeSpirit.Settings.Data;
using CodeSpirit.Settings.Models;
using CodeSpirit.Settings.Services.Implementations;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

namespace CodeSpirit.Settings.Tests.TestBase
{
    /// <summary>
    /// 设置服务测试基类
    /// </summary>
    public abstract class SettingsServiceTestBase : IDisposable
    {
        protected readonly string DatabaseName;
        protected SettingsDbContext DbContext = null!;
        protected ServiceProvider ServiceProvider = null!;
        
        // 常用的Mock对象
        protected Mock<ILogger<SettingsService>> MockSettingsServiceLogger = null!;
        protected Mock<IHttpContextAccessor> MockHttpContextAccessor = null!;
        protected Mock<ICurrentUser> MockCurrentUser = null!;
        protected Mock<IDataFilter> MockDataFilter = null!;
        protected Mock<IDistributedCache> MockDistributedCache = null!;
        
        // 数据仓库
        protected Repository<SettingItem> SettingItemRepository = null!;
        protected Repository<SettingHistory> SettingHistoryRepository = null!;
        
        // 缓存键值映射
        public Dictionary<string, byte[]> CacheItems { get; } = new Dictionary<string, byte[]>();
        
        protected SettingsServiceTestBase()
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
            MockSettingsServiceLogger = new Mock<ILogger<SettingsService>>();
            MockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            MockCurrentUser = new Mock<ICurrentUser>();
            MockDataFilter = new Mock<IDataFilter>();
            MockDistributedCache = new Mock<IDistributedCache>();
            
            // 配置分布式缓存模拟 - 实现完整的缓存行为
            MockDistributedCache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken token) => 
                {
                    if (CacheItems.TryGetValue(key, out var value))
                    {
                        return Task.FromResult(value);
                    }
                    return Task.FromResult<byte[]>(null);
                });

            // 实现SetAsync方法，存储缓存项
            MockDistributedCache
                .Setup(x => x.SetAsync(
                    It.IsAny<string>(), 
                    It.IsAny<byte[]>(), 
                    It.IsAny<DistributedCacheEntryOptions>(), 
                    It.IsAny<CancellationToken>()))
                .Callback((string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token) => 
                {
                    CacheItems[key] = value;
                })
                .Returns(Task.CompletedTask);
                    
            // 实现RemoveAsync方法，删除缓存项
            MockDistributedCache
                .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((string key, CancellationToken token) => 
                {
                    if (CacheItems.ContainsKey(key))
                    {
                        CacheItems.Remove(key);
                    }
                })
                .Returns(Task.CompletedTask);
            
            // 配置HttpContextAccessor模拟登录用户
            var httpContext = new Mock<HttpContext>();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "TestUser")
            }));
            httpContext.Setup(x => x.User).Returns(claimsPrincipal);
            MockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
            
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
            services.AddDbContext<SettingsDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
            });
            
            // 注册必要的服务
            services.AddSingleton(MockHttpContextAccessor.Object);
            services.AddSingleton(MockCurrentUser.Object);
            services.AddSingleton(MockDataFilter.Object);
            services.AddSingleton(MockDistributedCache.Object);
            
            // 添加日志工厂
            services.AddLogging(builder => builder.AddDebug());
            
            // 创建服务提供者
            ServiceProvider = services.BuildServiceProvider();
            
            // 创建DbContext
            DbContext = ServiceProvider.GetRequiredService<SettingsDbContext>();
        }
        
        /// <summary>
        /// 初始化仓库
        /// </summary>
        protected virtual void InitializeRepositories()
        {
            SettingItemRepository = new Repository<SettingItem>(DbContext);
            SettingHistoryRepository = new Repository<SettingHistory>(DbContext);
        }
        
        /// <summary>
        /// 添加测试数据
        /// </summary>
        protected virtual void SeedTestData()
        {
            // 在子类中实现具体的测试数据填充
        }
        
        /// <summary>
        /// 添加测试设置项数据
        /// </summary>
        /// <param name="settingItems">设置项列表</param>
        protected void SeedSettingItems(params SettingItem[] settingItems)
        {
            foreach (var item in settingItems)
            {
                DbContext.Set<SettingItem>().Add(item);
            }
            DbContext.SaveChanges();
            ClearDbContext();
        }
        
        /// <summary>
        /// 添加测试设置历史数据
        /// </summary>
        /// <param name="settingHistories">设置历史列表</param>
        protected void SeedSettingHistories(params SettingHistory[] settingHistories)
        {
            foreach (var history in settingHistories)
            {
                DbContext.Set<SettingHistory>().Add(history);
            }
            DbContext.SaveChanges();
            ClearDbContext();
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
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>仓库实例</returns>
        protected Repository<T> CreateRepository<T>() where T : class
        {
            return new Repository<T>(DbContext);
        }
        
        /// <summary>
        /// 创建IRepository<T>的Mock实例
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>Mock的仓库实例</returns>
        protected Mock<IRepository<T>> CreateMockRepository<T>() where T : class
        {
            return new Mock<IRepository<T>>();
        }
        
        /// <summary>
        /// 为缓存设置模拟值
        /// </summary>
        protected void MockCachedValue(string key, string value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            var data = Encoding.UTF8.GetBytes(jsonValue);
            CacheItems[key] = data;
        }
        
        /// <summary>
        /// 检查缓存键是否存在
        /// </summary>
        protected bool CacheKeyExists(string key)
        {
            return CacheItems.ContainsKey(key);
        }
        
        /// <summary>
        /// 获取缓存值
        /// </summary>
        protected string GetCachedValue(string key)
        {
            if (CacheItems.TryGetValue(key, out var bytes) && bytes != null)
            {
                var jsonValue = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<string>(jsonValue);
            }
            return null;
        }
    }
} 