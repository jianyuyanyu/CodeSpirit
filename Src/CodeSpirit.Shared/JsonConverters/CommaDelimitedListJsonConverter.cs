using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.Shared.JsonConverters;

public class CommaDelimitedListJsonConverter : JsonConverter<List<long>>
{
    public override List<long> ReadJson(JsonReader reader, Type objectType, List<long> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return new List<long>();
            }

            try
            {
                return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => long.Parse(s.Trim()))
                    .ToList();
            }
            catch
            {
                throw new JsonSerializationException("无效的ID格式");
            }
        }
        else if (reader.TokenType == JsonToken.StartArray)
        {
            var list = new List<long>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }
                if (reader.TokenType == JsonToken.Integer)
                {
                    list.Add(Convert.ToInt64(reader.Value));
                }
            }
            return list;
        }

        throw new JsonSerializationException("无效的JSON格式");
    }

    public override void WriteJson(JsonWriter writer, List<long> value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteValue(item);
        }
        writer.WriteEndArray();
    }
} 