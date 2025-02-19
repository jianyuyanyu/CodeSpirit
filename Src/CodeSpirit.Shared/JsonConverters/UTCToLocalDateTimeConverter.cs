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

            // 保持原样,让 Model Binder 处理输入转换
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
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                // UTC时间转换为本地时间
                dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.Local);
            }

            writer.WriteValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
