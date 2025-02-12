using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CodeSpirit.Core.IdGenerator
{
    /// <summary>
    /// 适用于K8s环境的雪花ID生成器
    /// </summary>
    public class SnowflakeIdGenerator : IIdGenerator
    {
        private static readonly object Lock = new();
        private const long Twepoch = 1288834974657L; // 起始时间戳：2010-11-04 09:42:54.657

        private const int WorkerIdBits = 10;  // 工作节点ID位数（使用Pod IP后10位）
        private const int SequenceBits = 12;  // 序列号位数

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);  // 最大工作节点ID

        private const int WorkerIdShift = SequenceBits;  // 工作节点ID左移位数
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits;  // 时间戳左移位数
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);  // 序列号掩码

        private readonly long _workerId;  // 工作节点ID
        private long _sequence;  // 序列号
        private long _lastTimestamp = -1L;  // 上次生成ID的时间戳

        /// <summary>
        /// 构造函数 - 自动从Pod IP获取工作节点ID
        /// </summary>
        public SnowflakeIdGenerator()
        {
            _workerId = GetWorkerIdFromPodIP();
        }

        /// <summary>
        /// 从Pod IP获取工作节点ID
        /// </summary>
        private static long GetWorkerIdFromPodIP()
        {
            try
            {
                // 获取本机IP地址
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                IPAddress ipAddress = addresses.FirstOrDefault(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip));

                if (ipAddress == null)
                {
                    throw new Exception("无法获取有效的IP地址");
                }

                // 使用IP地址最后10位作为工作节点ID
                byte[] bytes = ipAddress.GetAddressBytes();
                long workerId = ((long)bytes[2] << 8) | bytes[3];
                workerId &= MaxWorkerId; // 确保在有效范围内

                return workerId;
            }
            catch (Exception ex)
            {
                // 如果获取IP失败，使用随机数作为备选方案
                Random random = new();
                long workerId = random.NextInt64(0, MaxWorkerId + 1);
                Console.WriteLine($"警告：无法从Pod IP获取工作节点ID，使用随机ID：{workerId}。错误：{ex.Message}");
                return workerId;
            }
        }

        /// <summary>
        /// 生成新的ID
        /// </summary>
        public long NewId()
        {
            lock (Lock)
            {
                long timestamp = TimeGen();

                if (timestamp < _lastTimestamp)
                {
                    // 时钟回拨处理：等待到达上次时间戳
                    timestamp = WaitNextMillis(_lastTimestamp);
                }

                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        timestamp = WaitNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Twepoch) << TimestampLeftShift) |
                       (_workerId << WorkerIdShift) |
                       _sequence;
            }
        }

        /// <summary>
        /// 等待到达下一毫秒
        /// </summary>
        private static long WaitNextMillis(long lastTimestamp)
        {
            long timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
                Thread.Sleep(0); // 让出CPU时间片
            }
            return timestamp;
        }

        /// <summary>
        /// 生成时间戳
        /// </summary>
        private static long TimeGen()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}