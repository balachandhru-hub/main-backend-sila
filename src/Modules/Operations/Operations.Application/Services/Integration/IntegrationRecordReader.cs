using System.Globalization;
using System.Text.Json;
using Operations.Domain.Entities;
using Operations.Domain.Enums;

namespace Operations.Application.Services.Integration
{
    /// <summary>
    /// Reads mapped values out of one record of an ERP payload (or of a spreadsheet row).
    /// </summary>
    public static class IntegrationRecordReader
    {
        public static string? Read(IReadOnlyList<ApiFieldMapping> mappings, string targetField, JsonElement record)
        {
            ApiFieldMapping? mapping = mappings.FirstOrDefault(item => item.TargetField.Equals(targetField, StringComparison.OrdinalIgnoreCase));
            return MapValue(mapping, record);
        }

        public static string? MapValue(ApiFieldMapping? mapping, JsonElement source)
        {
            if (mapping == null)
            {
                return null;
            }

            JsonElement? value = ReadPath(source, mapping.SourceField);
            if (value == null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return mapping.NullPolicy == IntegrationNullPolicy.DEFAULT_VALUE ? mapping.DefaultValue : null;
            }

            string? text = value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() : value.Value.ToString();
            return mapping.Transformation switch
            {
                "TRIM" => text?.Trim(),
                "UPPER" => text?.Trim().ToUpperInvariant(),
                "LOWER" => text?.Trim().ToLowerInvariant(),
                _ => text
            };
        }

        public static JsonElement? ReadPath(JsonElement source, string path)
        {
            JsonElement current = source;
            foreach (string segment in path.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current;
        }

        public static DateTime? ReadDate(JsonElement record, string? field)
        {
            return string.IsNullOrWhiteSpace(field) ? null : ParseDateTime(ReadPath(record, field)?.ToString());
        }

        public static DateOnly? ParseDate(string? value)
        {
            return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result) ? result : null;
        }

        public static DateTime? ParseDateTime(string? value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime result) ? result.ToUniversalTime() : null;
        }

        public static int? ParseInt(string? value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : null;
        }

        public static decimal? ParseDecimal(string? value)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result) ? result : null;
        }

        public static DateTime? Max(DateTime? first, DateTime? second)
        {
            return second == null || first >= second ? first : second;
        }
    }
}
