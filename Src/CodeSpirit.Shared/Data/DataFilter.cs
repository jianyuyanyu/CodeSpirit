using CodeSpirit.Shared.Extensions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CodeSpirit.Shared.Data
{
    public class DataFilter : IDataFilter
    {
        private readonly ConcurrentDictionary<Type, object> _filters;

        private readonly IServiceProvider _serviceProvider;

        public DataFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _filters = new ConcurrentDictionary<Type, object>();
        }

        public IDisposable Enable<TFilter>()
            where TFilter : class
        {
            return GetFilter<TFilter>().Enable();
        }

        public IDisposable Disable<TFilter>()
            where TFilter : class
        {
            return GetFilter<TFilter>().Disable();
        }

        public bool IsEnabled<TFilter>()
            where TFilter : class
        {
            return GetFilter<TFilter>().IsEnabled;
        }

        private IDataFilter<TFilter> GetFilter<TFilter>()
            where TFilter : class
        {
            try
            {
                return _filters.GetOrAdd(
                    typeof(TFilter),
                    factory: _ =>
                    {
                        var filter = _serviceProvider.GetRequiredService<IDataFilter<TFilter>>();
                        // 可以添加日志记录
                        return filter;
                    }
                ) as IDataFilter<TFilter> ?? 
                   throw new InvalidOperationException($"无法创建过滤器实例：{typeof(TFilter).Name}");
            }
            catch (Exception ex)
            {
                // 记录日志并抛出更有意义的异常
                throw new InvalidOperationException($"创建数据过滤器失败：{typeof(TFilter).Name}", ex);
            }
        }
    }

    public class DataFilter<TFilter> : IDataFilter<TFilter>
        where TFilter : class
    {
        public bool IsEnabled
        {
            get
            {
                if (_filter.Value == null)
                {
                    // 延迟初始化，使用配置的默认值
                    var defaultState = _options.DefaultStates.GetOrDefault(typeof(TFilter));
                    _filter.Value = defaultState?.Clone() ?? new DataFilterState(false); // 使用更安全的默认值
                }
                return _filter.Value.IsEnabled;
            }
        }

        private readonly DataFilterOptions _options;

        private readonly AsyncLocal<DataFilterState> _filter;

        public DataFilter(IOptions<DataFilterOptions> options)
        {
            _options = options.Value;
            _filter = new AsyncLocal<DataFilterState>();
        }

        public IDisposable Enable()
        {
            EnsureInitialized();
            var previousState = _filter.Value.IsEnabled;
            
            if (previousState)
            {
                return NullDisposable.Instance;
            }

            _filter.Value.IsEnabled = true;
            return new DisposeAction(() => { _filter.Value.IsEnabled = previousState; });
        }

        public IDisposable Disable()
        {
            EnsureInitialized();
            var previousState = _filter.Value.IsEnabled;
            
            if (!previousState)
            {
                return NullDisposable.Instance;
            }

            _filter.Value.IsEnabled = false;
            return new DisposeAction(() => { _filter.Value.IsEnabled = previousState; });
        }

        private void EnsureInitialized()
        {
            if (_filter.Value != null)
            {
                return;
            }

            _filter.Value = _options.DefaultStates.GetOrDefault(typeof(TFilter))?.Clone() ?? new DataFilterState(true);
        }
    }
}
