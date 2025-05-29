using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.MultiTenant.Tests.Examples;

/// <summary>
/// 多租户使用示例
/// </summary>
public static class MultiTenantExample
{
    /// <summary>
    /// 配置多租户服务示例
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. 注册基础服务
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // 2. 注册多租户服务
        services.AddCodeSpiritMultiTenant(configuration);

        // 3. 注册数据库上下文工厂（支持多租户）
        services.AddDbContextFactory<ExampleDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        // 4. 注册业务服务
        services.AddScoped<IOrderService, OrderService>();
    }

    /// <summary>
    /// 配置中间件示例
    /// </summary>
    public static void ConfigureMiddleware(IApplicationBuilder app)
    {
        // 使用多租户中间件（应该在路由之前）
        app.UseCodeSpiritMultiTenant();
        
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

/// <summary>
/// 多租户实体示例
/// </summary>
public class Order : IMultiTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 多租户数据库上下文示例
/// </summary>
public class ExampleDbContext : DbContext
{
    private readonly ITenantResolver _tenantResolver;

    public ExampleDbContext(DbContextOptions<ExampleDbContext> options, ITenantResolver tenantResolver)
        : base(options)
    {
        _tenantResolver = tenantResolver;
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置多租户实体的全局查询过滤器
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
    }

    private string GetCurrentTenantId()
    {
        // 在查询时获取当前租户ID
        var tenantId = _tenantResolver.ResolveTenantIdAsync().GetAwaiter().GetResult();
        return tenantId ?? "default";
    }

    public override int SaveChanges()
    {
        SetTenantId();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var tenantId = _tenantResolver.ResolveTenantIdAsync().GetAwaiter().GetResult() ?? "default";

        foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}

/// <summary>
/// 订单服务接口
/// </summary>
public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(Order order);
    Task<Order?> UpdateOrderAsync(int id, Order order);
    Task<bool> DeleteOrderAsync(int id);
}

/// <summary>
/// 订单服务实现（支持多租户）
/// </summary>
public class OrderService : IOrderService
{
    private readonly IDbContextFactory<ExampleDbContext> _dbContextFactory;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IDbContextFactory<ExampleDbContext> dbContextFactory,
        ITenantResolver tenantResolver,
        ILogger<OrderService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        _logger.LogInformation("获取租户 {TenantId} 的订单列表", tenantInfo?.TenantId);

        using var context = _dbContextFactory.CreateDbContext();
        return await context.Orders.ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        _logger.LogInformation("获取租户 {TenantId} 的订单 {OrderId}", tenantInfo?.TenantId, id);

        using var context = _dbContextFactory.CreateDbContext();
        return await context.Orders.FindAsync(id);
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        _logger.LogInformation("为租户 {TenantId} 创建订单", tenantInfo?.TenantId);

        using var context = _dbContextFactory.CreateDbContext();
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateOrderAsync(int id, Order order)
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        _logger.LogInformation("更新租户 {TenantId} 的订单 {OrderId}", tenantInfo?.TenantId, id);

        using var context = _dbContextFactory.CreateDbContext();
        var existingOrder = await context.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return null;
        }

        existingOrder.ProductName = order.ProductName;
        existingOrder.Amount = order.Amount;
        await context.SaveChangesAsync();
        return existingOrder;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        _logger.LogInformation("删除租户 {TenantId} 的订单 {OrderId}", tenantInfo?.TenantId, id);

        using var context = _dbContextFactory.CreateDbContext();
        var order = await context.Orders.FindAsync(id);
        if (order == null)
        {
            return false;
        }

        context.Orders.Remove(order);
        await context.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// 订单控制器示例（支持多租户）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITenantResolver _tenantResolver;

    public OrdersController(IOrderService orderService, ITenantResolver tenantResolver)
    {
        _orderService = orderService;
        _tenantResolver = tenantResolver;
    }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    [HttpGet("tenant")]
    public async Task<IActionResult> GetTenantInfo()
    {
        var tenantInfo = await _tenantResolver.GetCurrentTenantInfoAsync();
        if (tenantInfo == null)
        {
            return BadRequest("无法解析租户信息");
        }

        return Ok(new
        {
            tenantInfo.TenantId,
            tenantInfo.Name,
            tenantInfo.Strategy,
            tenantInfo.IsActive
        });
    }

    /// <summary>
    /// 获取订单列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _orderService.GetOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// 根据ID获取订单
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }

    /// <summary>
    /// 创建订单
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        var createdOrder = await _orderService.CreateOrderAsync(order);
        return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
    }

    /// <summary>
    /// 更新订单
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
    {
        var updatedOrder = await _orderService.UpdateOrderAsync(id, order);
        if (updatedOrder == null)
        {
            return NotFound();
        }
        return Ok(updatedOrder);
    }

    /// <summary>
    /// 删除订单
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var result = await _orderService.DeleteOrderAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
} 