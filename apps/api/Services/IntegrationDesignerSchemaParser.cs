using System.Text.Json;
using System.Xml.Linq;
using SilaMe.Api.DTOs;

namespace SilaMe.Api.Services;

public static class IntegrationDesignerSchemaParser
{
    public static IReadOnlyList<IntegrationSchemaEntity> ParseOData(string xml)
    {
        var document = XDocument.Parse(xml);
        var entitySets = document.Descendants().Where(item => item.Name.LocalName == "EntitySet")
            .Select(item => new { Name = (string?)item.Attribute("Name"), Type = ((string?)item.Attribute("EntityType"))?.Split('.').Last() })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .ToDictionary(item => item.Type ?? item.Name!, item => item.Name!, StringComparer.OrdinalIgnoreCase);
        return document.Descendants().Where(item => item.Name.LocalName == "EntityType").Select(entity =>
        {
            var name = (string?)entity.Attribute("Name") ?? string.Empty;
            var properties = entity.Elements().Where(item => item.Name.LocalName == "Property")
                .Select(property => new IntegrationSchemaProperty((string?)property.Attribute("Name") ?? string.Empty, ((string?)property.Attribute("Type") ?? "Edm.String").Replace("Edm.", string.Empty), (string?)property.Attribute("Nullable") != "false")).ToList();
            var keys = entity.Descendants().Where(item => item.Name.LocalName == "PropertyRef").Select(item => (string?)item.Attribute("Name") ?? string.Empty).Where(item => item.Length > 0).ToList();
            entitySets.TryGetValue(name, out var entitySet);
            return new IntegrationSchemaEntity(name, entitySet, properties, keys);
        }).ToList();
    }

    public static IReadOnlyList<IntegrationSchemaEntity> ParseWsdl(string xml)
    {
        var document = XDocument.Parse(xml);
        return document.Descendants().Where(item => item.Name.LocalName == "operation")
            .Select(item => (string?)item.Attribute("name") ?? string.Empty)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new IntegrationSchemaEntity(name, name, [new IntegrationSchemaProperty("request", "complex", false)], []))
            .ToList();
    }

    public static IReadOnlyList<IntegrationSchemaEntity> ParseXmlSample(string xml)
    {
        var document = XDocument.Parse(xml);
        var root = document.Root ?? throw new IntegrationException("SCHEMA_SAMPLE_INVALID", "The sample XML could not be parsed.");
        var properties = root.Elements().Select(item => new IntegrationSchemaProperty(item.Name.LocalName, "string", true)).ToList();
        return [new IntegrationSchemaEntity(root.Name.LocalName, null, properties, [])];
    }

    public static IReadOnlyList<IntegrationSchemaEntity> ParseJsonSample(string json)
    {
        using var document = JsonDocument.Parse(json);
        return [new IntegrationSchemaEntity("Request", null, ReadProperties(document.RootElement, ""), [])];
    }

    public static IReadOnlyList<IntegrationSchemaEntity> ParseOpenApi(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("components", out var components) && components.TryGetProperty("schemas", out var schemas))
        {
            return schemas.EnumerateObject().Select(schema =>
                new IntegrationSchemaEntity(schema.Name, schema.Name, ReadObjectProperties(schema.Value), [])).ToList();
        }
        if (document.RootElement.TryGetProperty("paths", out var paths))
        {
            return [new IntegrationSchemaEntity("Paths", null, paths.EnumerateObject().Select(path => new IntegrationSchemaProperty(path.Name, "path", true)).ToList(), [])];
        }
        return ParseJsonSample(json);
    }

    private static List<IntegrationSchemaProperty> ReadObjectProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("properties", out var properties))
            return properties.EnumerateObject().Select(property => new IntegrationSchemaProperty(property.Name, property.Value.TryGetProperty("type", out var type) ? type.GetString() ?? "object" : "object", true)).ToList();
        return ReadProperties(element, "");
    }

    private static List<IntegrationSchemaProperty> ReadProperties(JsonElement element, string prefix)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
            return ReadProperties(element[0], prefix.Length == 0 ? "items" : prefix + "[]");
        if (element.ValueKind != JsonValueKind.Object) return [new IntegrationSchemaProperty(string.IsNullOrEmpty(prefix) ? "value" : prefix, element.ValueKind.ToString().ToLowerInvariant(), true)];
        var properties = new List<IntegrationSchemaProperty>();
        foreach (var property in element.EnumerateObject())
        {
            var name = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                properties.AddRange(ReadProperties(property.Value, name));
            else
                properties.Add(new IntegrationSchemaProperty(name, property.Value.ValueKind.ToString().ToLowerInvariant(), true));
        }
        return properties;
    }
}
