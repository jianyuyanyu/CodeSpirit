using Newtonsoft.Json;

namespace CodeSpirit.Shared.JsonConverters
{
    public class LongToStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(long) || objectType == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonToken.String)
            {
                if (long.TryParse((string)reader.Value, out long result))
                {
                    return result;
                }
            }
            return reader.TokenType == JsonToken.Integer
                ? (object)Convert.ToInt64(reader.Value)
                : throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.ToString());
            }
        }
    }
}
