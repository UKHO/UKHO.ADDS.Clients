using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Infrastructure.Serialization.Json
{
    public sealed class JsonCodec
    {
        private static readonly JsonSerializerOptions _defaultOptions;
        private static readonly JsonSerializerOptions _defaultOptionsNoFormat;

        static JsonCodec()
        {
            _defaultOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true)
                }
            };

            _defaultOptionsNoFormat = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true)
                }
            };
        }

        public static string Encode<T>(T value) => Encode(value, _defaultOptions);

        public static string Encode<T>(T value, JsonSerializerOptions options) => JsonSerializer.Serialize(value, options);

        public static T? Decode<T>(string json) => Decode<T>(json, _defaultOptions);

        public static T? Decode<T>(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options);

        public static JsonSerializerOptions DefaultOptions => _defaultOptions;

        public static JsonSerializerOptions DefaultOptionsNoFormat => _defaultOptionsNoFormat;
    }
}
