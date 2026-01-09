using CronExpressionDescriptor;
using Cronos;

namespace CodeSpirit.ScheduledTasks.Helpers;

/// <summary>
/// Cron表达式辅助类
/// </summary>
public static class CronHelper
{
    /// <summary>
    /// 验证Cron表达式
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <returns>是否有效</returns>
    public static bool IsValidCronExpression(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        try
        {
            CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取下次执行时间
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <param name="fromTime">起始时间</param>
    /// <returns>下次执行时间</returns>
    public static DateTime? GetNextOccurrence(string cronExpression, DateTime? fromTime = null)
    {
        if (!IsValidCronExpression(cronExpression))
        {
            return null;
        }

        try
        {
            var cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            var baseTime = fromTime ?? DateTime.UtcNow;
            return cron.GetNextOccurrence(baseTime, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取多个下次执行时间
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <param name="count">获取数量</param>
    /// <param name="fromTime">起始时间</param>
    /// <returns>执行时间列表</returns>
    public static List<DateTime> GetNextOccurrences(string cronExpression, int count, DateTime? fromTime = null)
    {
        var results = new List<DateTime>();

        if (!IsValidCronExpression(cronExpression) || count <= 0)
        {
            return results;
        }

        try
        {
            var cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            var baseTime = fromTime ?? DateTime.UtcNow;
            
            var currentTime = baseTime;
            for (int i = 0; i < count; i++)
            {
                var nextOccurrence = cron.GetNextOccurrence(currentTime, TimeZoneInfo.Utc);
                if (nextOccurrence.HasValue)
                {
                    results.Add(nextOccurrence.Value);
                    currentTime = nextOccurrence.Value.AddSeconds(1); // 确保下次查找时间在当前时间之后
                }
                else
                {
                    break; // 没有更多的执行时间
                }
            }
        }
        catch
        {
            // 忽略异常，返回空列表
        }

        return results;
    }

    /// <summary>
    /// 获取Cron表达式描述
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <param name="locale">语言区域（默认中文）</param>
    /// <returns>描述文本</returns>
    public static string GetDescription(string cronExpression, string locale = "zh-Hans")
    {
        if (!IsValidCronExpression(cronExpression))
        {
            return "无效的Cron表达式";
        }

        try
        {
            // 使用 CronExpressionDescriptor 生成人性化描述
            var options = new CronExpressionDescriptor.Options
            {
                Locale = locale,
                Use24HourTimeFormat = true,
                Verbose = false
            };
            
            return ExpressionDescriptor.GetDescription(cronExpression, options);
        }
        catch (Exception)
        {
            // 如果描述失败，返回原始表达式
            return $"Cron表达式: {cronExpression}";
        }
    }

    /// <summary>
    /// 获取Cron表达式的详细描述
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <param name="locale">语言区域（默认中文）</param>
    /// <returns>详细描述文本</returns>
    public static string GetVerboseDescription(string cronExpression, string locale = "zh-Hans")
    {
        if (!IsValidCronExpression(cronExpression))
        {
            return "无效的Cron表达式";
        }

        try
        {
            var options = new CronExpressionDescriptor.Options
            {
                Locale = locale,
                Use24HourTimeFormat = true,
                Verbose = true
            };
            
            return ExpressionDescriptor.GetDescription(cronExpression, options);
        }
        catch (Exception)
        {
            return $"Cron表达式: {cronExpression}";
        }
    }

    /// <summary>
    /// 常用Cron表达式预设
    /// </summary>
    public static class Presets
    {
        /// <summary>
        /// 每秒执行
        /// </summary>
        public const string EverySecond = "* * * * * *";

        /// <summary>
        /// 每分钟执行
        /// </summary>
        public const string EveryMinute = "0 * * * * *";

        /// <summary>
        /// 每小时执行
        /// </summary>
        public const string EveryHour = "0 0 * * * *";

        /// <summary>
        /// 每天执行（凌晨0点）
        /// </summary>
        public const string Daily = "0 0 0 * * *";

        /// <summary>
        /// 每周执行（周一凌晨0点）
        /// </summary>
        public const string Weekly = "0 0 0 * * 1";

        /// <summary>
        /// 每月执行（1号凌晨0点）
        /// </summary>
        public const string Monthly = "0 0 0 1 * *";

        /// <summary>
        /// 工作日每天执行（周一到周五凌晨0点）
        /// </summary>
        public const string Weekdays = "0 0 0 * * 1-5";

        /// <summary>
        /// 每5分钟执行
        /// </summary>
        public const string Every5Minutes = "0 */5 * * * *";

        /// <summary>
        /// 每15分钟执行
        /// </summary>
        public const string Every15Minutes = "0 */15 * * * *";

        /// <summary>
        /// 每30分钟执行
        /// </summary>
        public const string Every30Minutes = "0 */30 * * * *";

        /// <summary>
        /// 每天上午9点执行
        /// </summary>
        public const string DailyAt9AM = "0 0 9 * * *";

        /// <summary>
        /// 每天下午6点执行
        /// </summary>
        public const string DailyAt6PM = "0 0 18 * * *";

        /// <summary>
        /// 获取所有预设
        /// </summary>
        /// <returns>预设字典</returns>
        public static Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>
            {
                { "每秒执行", EverySecond },
                { "每分钟执行", EveryMinute },
                { "每小时执行", EveryHour },
                { "每天执行", Daily },
                { "每周执行", Weekly },
                { "每月执行", Monthly },
                { "工作日执行", Weekdays },
                { "每5分钟执行", Every5Minutes },
                { "每15分钟执行", Every15Minutes },
                { "每30分钟执行", Every30Minutes },
                { "每天上午9点", DailyAt9AM },
                { "每天下午6点", DailyAt6PM }
            };
        }
    }
}
