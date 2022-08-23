using Framework.Core.Dependency;
using Framework.Timing;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Framework.Json.SystemTextJson.JsonConverters
{
    /// <summary>
    /// SystemTextJson DateTime?类型转化
    /// </summary>
    public class NullableDateTimeConverter : JsonConverter<DateTime?>, ITransient
    {
        private readonly IClock _clock;
        private readonly JsonOptions _options;

        public NullableDateTimeConverter(IClock clock, IOptions<JsonOptions> jsonOptions)
        {
            _clock = clock;
            _options = jsonOptions.Value;
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!string.IsNullOrWhiteSpace(_options.DefaultDateTimeFormat))
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    if (DateTime.TryParseExact(s, _options.DefaultDateTimeFormat, CultureInfo.CurrentUICulture, DateTimeStyles.None, out var d1))
                    {
                        return _clock.Normalize(d1);
                    }
                    else {
                        //默认的解析不了,试试扩展时间format
                        foreach (var extraFormat in _options.ExtraDateTimeFormats)
                        {
                            if (DateTime.TryParseExact(s, extraFormat, CultureInfo.CurrentUICulture, DateTimeStyles.None, out var d6))
                            {
                                return _clock.Normalize(d6);
                            }
                        }
                    }

                    throw new JsonException($"'{s}' can't parse to DateTime({_options.DefaultDateTimeFormat})!");
                }

                throw new JsonException("Reader's TokenType is not String!");
            }

            if (reader.TryGetDateTime(out var d2))
            {
                return _clock.Normalize(d2);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_options.DefaultDateTimeFormat))
                {
                    writer.WriteStringValue(_clock.Normalize(value.Value));
                }
                else
                {
                    writer.WriteStringValue(_clock.Normalize(value.Value).ToString(_options.DefaultDateTimeFormat, CultureInfo.CurrentUICulture));
                }
            }
        }
    }
}
