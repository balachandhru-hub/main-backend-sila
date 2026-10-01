using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operations.API.Extensions
{
    /// <summary>
    /// Every timestamp of this service is stored in UTC, but SQL Server's datetime2 returns it
    /// without a kind. This converter writes such values as UTC (with the trailing "Z"), so a
    /// browser never reads them as local time.
    /// </summary>
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DateTime value = reader.GetDateTime();
            return value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            DateTime utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            writer.WriteStringValue(utc);
        }
    }
}
