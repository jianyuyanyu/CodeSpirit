using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CodeSpirit.Shared.JsonConverters
{
    public class UTCToLocalDateTimeConverter : DateTimeConverterBase
    {
        public override bool CanConvert(Type objectType)
        {
            // 只处理 DateTime 类型，忽略 DateTimeOffset
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            // 将输入字符串解析为 DateTime
            if (DateTime.TryParse(reader.Value.ToString(), out DateTime dateTime))
            {
                // 将解析后的时间视为本地时间
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                
                // 将本地时间转换为 UTC 时间
                return TimeZoneInfo.ConvertTimeToUtc(dateTime);
            }
            
            // 如果解析失败，返回原始值（兼容原有逻辑）
            return DateTime.Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            DateTime dateTime = (DateTime)value;
            // 假设所有时间都是UTC时间（除非明确指定为Local）
            if (dateTime.Kind != DateTimeKind.Local)
            {
                // 将时间视为UTC并转换为本地时间
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.Local);
            }

            writer.WriteValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
