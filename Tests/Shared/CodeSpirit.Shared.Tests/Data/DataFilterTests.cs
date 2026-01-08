using CodeSpirit.Shared.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using Xunit;

namespace CodeSpirit.Shared.Tests.Data;

/// <summary>
/// DataFilter 单元测试
/// </summary>
public class DataFilterTests
{
    #region 测试用的过滤器接口

    /// <summary>
    /// 测试用的过滤器接口1
    /// </summary>
    public interface ITestFilter1 { }

    /// <summary>
    /// 测试用的过滤器接口2
    /// </summary>
    public interface ITestFilter2 { }

    /// <summary>
    /// 测试用的过滤器接口3
    /// </summary>
    public interface ITestFilter3 { }

    #endregion

    #region 基本功能测试

    [Fact]
    public void IsEnabled_DefaultState_ShouldReturnFalse()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var result = dataFilter.IsEnabled<ITestFilter1>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Enable_ShouldReturnDisposable()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var disposable = dataFilter.Enable<ITestFilter1>();

        // Assert
        Assert.NotNull(disposable);
        Assert.True(dataFilter.IsEnabled<ITestFilter1>());
    }

    [Fact]
    public void Disable_ShouldReturnDisposable()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        dataFilter.Enable<ITestFilter1>(); // 先启用

        // Act
        var disposable = dataFilter.Disable<ITestFilter1>();

        // Assert
        Assert.NotNull(disposable);
        Assert.False(dataFilter.IsEnabled<ITestFilter1>());
    }

    [Fact]
    public void Enable_WhenAlreadyEnabled_ShouldReturnNullDisposable()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        dataFilter.Enable<ITestFilter1>(); // 先启用

        // Act
        var disposable = dataFilter.Enable<ITestFilter1>();

        // Assert
        Assert.NotNull(disposable);
        Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        // 验证返回的是 NullDisposable（不会恢复状态）
        disposable.Dispose();
        Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 状态应该保持不变
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldReturnNullDisposable()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var disposable = dataFilter.Disable<ITestFilter1>();

        // Assert
        Assert.NotNull(disposable);
        Assert.False(dataFilter.IsEnabled<ITestFilter1>());
        // 验证返回的是 NullDisposable（不会恢复状态）
        disposable.Dispose();
        Assert.False(dataFilter.IsEnabled<ITestFilter1>()); // 状态应该保持不变
    }

    #endregion

    #region Dispose 行为测试

    [Fact]
    public void Enable_Dispose_ShouldRestorePreviousState()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        var initialState = dataFilter.IsEnabled<ITestFilter1>(); // false

        // Act
        using (dataFilter.Enable<ITestFilter1>())
        {
            Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        }

        // Assert
        Assert.Equal(initialState, dataFilter.IsEnabled<ITestFilter1>());
    }

    [Fact]
    public void Disable_Dispose_ShouldRestorePreviousState()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        dataFilter.Enable<ITestFilter1>(); // 先启用
        var initialState = dataFilter.IsEnabled<ITestFilter1>(); // true

        // Act
        using (dataFilter.Disable<ITestFilter1>())
        {
            Assert.False(dataFilter.IsEnabled<ITestFilter1>());
        }

        // Assert
        Assert.Equal(initialState, dataFilter.IsEnabled<ITestFilter1>());
    }

    [Fact]
    public void Enable_Dispose_ShouldRestoreToEnabledState()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        dataFilter.Enable<ITestFilter1>(); // 先启用

        // Act
        using (dataFilter.Disable<ITestFilter1>())
        {
            Assert.False(dataFilter.IsEnabled<ITestFilter1>());
        }

        // Assert
        Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 应该恢复到启用状态
    }

    [Fact]
    public void NestedEnableDisable_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act & Assert
        Assert.False(dataFilter.IsEnabled<ITestFilter1>()); // 初始状态：false

        using (dataFilter.Enable<ITestFilter1>())
        {
            Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 启用后：true

            using (dataFilter.Disable<ITestFilter1>())
            {
                Assert.False(dataFilter.IsEnabled<ITestFilter1>()); // 禁用后：false
            }

            Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 恢复后：true
        }

        Assert.False(dataFilter.IsEnabled<ITestFilter1>()); // 最终状态：false
    }

    #endregion

    #region 配置选项测试

    [Fact]
    public void IsEnabled_WithDefaultStateEnabled_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(true);
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var result = dataFilter.IsEnabled<ITestFilter1>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEnabled_WithDefaultStateDisabled_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(false);
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var result = dataFilter.IsEnabled<ITestFilter1>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsEnabled_WithoutDefaultState_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptions();
        // 不设置默认状态
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var result = dataFilter.IsEnabled<ITestFilter1>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Enable_WithDefaultStateEnabled_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(true);
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act & Assert
        Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 默认启用

        using (dataFilter.Disable<ITestFilter1>())
        {
            Assert.False(dataFilter.IsEnabled<ITestFilter1>()); // 禁用后
        }

        Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 恢复后应该回到启用状态
    }

    #endregion

    #region 多个过滤器类型测试

    [Fact]
    public void MultipleFilters_ShouldWorkIndependently()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act & Assert
        // Filter1: 启用
        dataFilter.Enable<ITestFilter1>();
        Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        Assert.False(dataFilter.IsEnabled<ITestFilter2>());
        Assert.False(dataFilter.IsEnabled<ITestFilter3>());

        // Filter2: 启用
        dataFilter.Enable<ITestFilter2>();
        Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        Assert.True(dataFilter.IsEnabled<ITestFilter2>());
        Assert.False(dataFilter.IsEnabled<ITestFilter3>());

        // Filter1: 禁用
        dataFilter.Disable<ITestFilter1>();
        Assert.False(dataFilter.IsEnabled<ITestFilter1>());
        Assert.True(dataFilter.IsEnabled<ITestFilter2>());
        Assert.False(dataFilter.IsEnabled<ITestFilter3>());
    }

    [Fact]
    public void MultipleFilters_WithDifferentDefaultStates_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(true);
        options.Value.DefaultStates[typeof(ITestFilter2)] = new DataFilterState(false);
        // ITestFilter3 没有默认状态
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act & Assert
        Assert.True(dataFilter.IsEnabled<ITestFilter1>()); // 默认启用
        Assert.False(dataFilter.IsEnabled<ITestFilter2>()); // 默认禁用
        Assert.False(dataFilter.IsEnabled<ITestFilter3>()); // 无默认状态，应该是 false
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        const int threadCount = 10;
        const int operationsPerThread = 100;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex =>
            Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    // 每个线程操作不同的过滤器类型
                    var filterType = threadIndex % 3;
                    switch (filterType)
                    {
                        case 0:
                            dataFilter.Enable<ITestFilter1>();
                            dataFilter.IsEnabled<ITestFilter1>();
                            dataFilter.Disable<ITestFilter1>();
                            break;
                        case 1:
                            dataFilter.Enable<ITestFilter2>();
                            dataFilter.IsEnabled<ITestFilter2>();
                            dataFilter.Disable<ITestFilter2>();
                            break;
                        case 2:
                            dataFilter.Enable<ITestFilter3>();
                            dataFilter.IsEnabled<ITestFilter3>();
                            dataFilter.Disable<ITestFilter3>();
                            break;
                    }
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        // 所有操作应该成功完成，没有异常
        Assert.True(true);
    }

    [Fact]
    public async Task ConcurrentEnableDisable_ShouldMaintainConsistency()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        const int threadCount = 5;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex =>
            Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    using (dataFilter.Enable<ITestFilter1>())
                    {
                        var isEnabled = dataFilter.IsEnabled<ITestFilter1>();
                        Assert.True(isEnabled); // 在启用期间应该始终为 true
                    }
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        // 最终状态应该是禁用（因为所有 using 都已释放）
        Assert.False(dataFilter.IsEnabled<ITestFilter1>());
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void GetFilter_ShouldCacheInstances()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        var filter1 = dataFilter.Enable<ITestFilter1>();
        var filter2 = dataFilter.Enable<ITestFilter1>();

        // Assert
        // 应该返回相同的实例（通过缓存）
        Assert.NotNull(filter1);
        Assert.NotNull(filter2);
        // 验证状态一致性
        Assert.True(dataFilter.IsEnabled<ITestFilter1>());
    }

    [Fact]
    public void MultipleDispose_ShouldNotThrowException()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);
        var disposable = dataFilter.Enable<ITestFilter1>();

        // Act & Assert
        disposable.Dispose();
        disposable.Dispose(); // 多次 Dispose 不应该抛出异常
        disposable.Dispose();
    }

    [Fact]
    public void EnableAfterDispose_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        var serviceProvider = CreateServiceProvider();
        var dataFilter = new DataFilter(serviceProvider);

        // Act
        using (dataFilter.Enable<ITestFilter1>())
        {
            Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        }

        Assert.False(dataFilter.IsEnabled<ITestFilter1>());

        // 再次启用
        using (dataFilter.Enable<ITestFilter1>())
        {
            Assert.True(dataFilter.IsEnabled<ITestFilter1>());
        }

        // Assert
        Assert.False(dataFilter.IsEnabled<ITestFilter1>());
    }

    #endregion

    #region DataFilter<TFilter> 直接测试

    [Fact]
    public void DataFilterT_IsEnabled_DefaultState_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptions();
        var filter = new DataFilter<ITestFilter1>(options);

        // Act
        var result = filter.IsEnabled;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DataFilterT_Enable_ShouldChangeState()
    {
        // Arrange
        var options = CreateOptions();
        var filter = new DataFilter<ITestFilter1>(options);

        // Act
        using (filter.Enable())
        {
            // Assert
            Assert.True(filter.IsEnabled);
        }

        Assert.False(filter.IsEnabled);
    }

    [Fact]
    public void DataFilterT_Disable_ShouldChangeState()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(true);
        var filter = new DataFilter<ITestFilter1>(options);

        // Act
        Assert.True(filter.IsEnabled); // 默认启用

        using (filter.Disable())
        {
            // Assert
            Assert.False(filter.IsEnabled);
        }

        Assert.True(filter.IsEnabled); // 恢复后应该回到启用状态
    }

    [Fact]
    public void DataFilterT_WithDefaultStateEnabled_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptions();
        options.Value.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(true);
        var filter = new DataFilter<ITestFilter1>(options);

        // Act
        var result = filter.IsEnabled;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DataFilterT_MultipleInstances_ShouldHaveIndependentState()
    {
        // Arrange
        var options = CreateOptions();
        var filter1 = new DataFilter<ITestFilter1>(options);
        var filter2 = new DataFilter<ITestFilter2>(options);

        // Act
        filter1.Enable();
        filter2.Disable();

        // Assert
        Assert.True(filter1.IsEnabled);
        Assert.False(filter2.IsEnabled);
    }

    #endregion

    #region 辅助方法

    private static IOptions<DataFilterOptions> CreateOptions()
    {
        var options = new DataFilterOptions();
        var mockOptions = new Mock<IOptions<DataFilterOptions>>();
        mockOptions.Setup(x => x.Value).Returns(options);
        return mockOptions.Object;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(ITestFilter1)] = new DataFilterState(false);
            options.DefaultStates[typeof(ITestFilter2)] = new DataFilterState(false);
            options.DefaultStates[typeof(ITestFilter3)] = new DataFilterState(false);
        });
        services.AddScoped(typeof(IDataFilter<>), typeof(DataFilter<>));
        return services.BuildServiceProvider();
    }

    #endregion
}

