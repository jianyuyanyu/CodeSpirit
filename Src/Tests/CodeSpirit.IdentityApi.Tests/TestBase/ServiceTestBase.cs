using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace CodeSpirit.IdentityApi.Tests.TestBase
{
    /// <summary>
    /// 服务测试基类
    /// </summary>
    public abstract class ServiceTestBase : IDisposable
    {
        protected readonly string DatabaseName;
        protected ApplicationDbContext DbContext;
        protected ServiceProvider ServiceProvider;
        
        // 常用的Mock对象
        protected Mock<ILogger<RoleService>> MockRoleServiceLogger;
        protected Mock<ILogger<UserService>> MockUserServiceLogger;
        protected Mock<ILogger<AuthService>> MockAuthServiceLogger;
        // 改用真实的Mapper，不再模拟
        protected IMapper Mapper;
        protected Mock<IHttpContextAccessor> MockHttpContextAccessor;
        protected Mock<ICurrentUser> MockCurrentUser;
        protected Mock<IDataFilter> MockDataFilter;
        
        // 用于Identity的实际服务
        protected UserManager<ApplicationUser> UserManager;
        protected RoleManager<ApplicationRole> RoleManager;
        protected SignInManager<ApplicationUser> SignInManager;
        
        // 数据仓库
        protected Repository<ApplicationUser> UserRepository;
        protected Repository<ApplicationRole> RoleRepository;
        protected Repository<LoginLog> LoginLogRepository;
        
        protected ServiceTestBase()
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
            MockRoleServiceLogger = new Mock<ILogger<RoleService>>();
            MockUserServiceLogger = new Mock<ILogger<UserService>>();
            MockAuthServiceLogger = new Mock<ILogger<AuthService>>();
            // 不再模拟Mapper，将在SetupInMemoryDatabase中配置真实的Mapper
            MockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            MockCurrentUser = new Mock<ICurrentUser>();
            MockDataFilter = new Mock<IDataFilter>();
            
            // 不再模拟 UserManager、RoleManager 和 SignInManager
            // 将使用真实的实现
                
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
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            
            // 添加 Identity 服务
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            // 添加AutoMapper配置
            services.AddAutoMapper(cfg => {
                // 注册来自 IdentityApi 的所有 Profile
                cfg.AddMaps(typeof(CodeSpirit.IdentityApi.MappingProfiles.RoleProfile).Assembly);
            });
            
            // 注册必要的服务
            services.AddSingleton(MockHttpContextAccessor.Object);
            services.AddSingleton(MockCurrentUser.Object);
            services.AddSingleton(MockDataFilter.Object);
            
            // 添加日志工厂和 Identity 所需的日志服务
            services.AddLogging(builder => builder.AddDebug());
            
            // 创建服务提供者
            ServiceProvider = services.BuildServiceProvider();
            
            // 创建DbContext
            DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 获取 Identity 服务
            UserManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager = ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            SignInManager = ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
            
            // 获取真实的 Mapper
            Mapper = ServiceProvider.GetRequiredService<IMapper>();
        }
        
        /// <summary>
        /// 初始化仓库
        /// </summary>
        protected virtual void InitializeRepositories()
        {
            UserRepository = new Repository<ApplicationUser>(DbContext);
            RoleRepository = new Repository<ApplicationRole>(DbContext);
            LoginLogRepository = new Repository<LoginLog>(DbContext);
        }
        
        /// <summary>
        /// 添加测试数据
        /// </summary>
        protected virtual void SeedTestData()
        {
            // 在子类中实现具体的测试数据填充
        }
        
        /// <summary>
        /// 添加测试用户数据
        /// </summary>
        /// <param name="users">用户列表</param>
        protected void SeedUsers(params ApplicationUser[] users)
        {
            DbContext.Set<ApplicationUser>().AddRange(users);
            DbContext.SaveChanges();
        }
        
        /// <summary>
        /// 添加测试角色数据
        /// </summary>
        /// <param name="roles">角色列表</param>
        protected void SeedRoles(params ApplicationRole[] roles)
        {
            DbContext.Set<ApplicationRole>().AddRange(roles);
            DbContext.SaveChanges();
        }
        
        /// <summary>
        /// 添加测试用户角色关系数据
        /// </summary>
        /// <param name="userRoles">用户角色关系列表</param>
        protected void SeedUserRoles(params ApplicationUserRole[] userRoles)
        {
            DbContext.Set<ApplicationUserRole>().AddRange(userRoles);
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
    }
} 