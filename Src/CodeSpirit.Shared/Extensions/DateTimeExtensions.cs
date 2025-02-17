namespace CodeSpirit.Shared.Extensions.Extensions
{
    public static class DateTimeExtensions
    {

        /// <summary>
        /// 本时区日期时间转时间戳
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns>long=Int64</returns>
        public static long ToTimestamp(this DateTime datetime)
        {
            DateTime dd = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime timeUTC = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);//本地时间转成UTC时间
            TimeSpan ts = timeUTC - dd;
            return (long)ts.TotalMilliseconds;//精确到毫秒
        }

        /// <summary>
        /// 时间戳转本时区日期时间
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(string timeStamp)
        {
            DateTime dd = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0, 0), DateTimeKind.Local);
            long longTimeStamp = long.Parse(timeStamp + "0000");
            TimeSpan ts = new TimeSpan(longTimeStamp);
            return dd.Add(ts);
        }


        /// <summary>
        /// 将毫秒级的Unix时间戳转时区日期时间 （ToUnixTimeMilliseconds）
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeMillisecondsToDateTime(long timeStamp)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间戳的起始时间
            DateTime time = startTime.AddMilliseconds(timeStamp);
            return time.ToLocalTime();
        }

        /// <summary>
        /// 获取最小秒数的日期 即 $"yyyy-MM-dd 00:00:00"
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DateTime FirstTimeOfDay(this DateTime data)
        {
            return data.Date;
        }

        /// <summary>
        /// 获取最大秒数的日期 即 $"yyyy-MM-dd 23:59:59.9999"
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DateTime LastTimeOfDay(this DateTime data)
        {
            return data.Date.AddDays(1).AddMilliseconds(-1);
        }
    }
}
