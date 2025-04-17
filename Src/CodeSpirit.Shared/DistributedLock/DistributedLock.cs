using System;
using System.Threading.Tasks;

namespace CodeSpirit.Shared.DistributedLock
{
    /// <summary>
    /// 分布式锁实现类
    /// </summary>
    public class DistributedLock : IDisposable
    {
        private readonly IDistributedLockProvider _provider;
        private readonly string _key;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="provider">分布式锁提供程序</param>
        /// <param name="key">锁的键名</param>
        public DistributedLock(IDistributedLockProvider provider, string key)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// 释放锁资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放锁资源
        /// </summary>
        /// <param name="disposing">是否为显式释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _provider.ReleaseLockAsync(_key).GetAwaiter().GetResult();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DistributedLock()
        {
            Dispose(false);
        }
    }
} 