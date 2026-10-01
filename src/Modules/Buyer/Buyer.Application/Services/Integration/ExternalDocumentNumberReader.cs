using System.Text.Json;

namespace Buyer.Application.Services.Integration
{
    public static class ExternalDocumentNumberReader
    {
        private static readonly string[] CandidateNames =
        {
            "purchaseOrderNumber",
            "poNumber",
            "documentNumber",
            "prNumber",
            "purchaseRequisitionNumber",
            "orderNumber",
            "orderNo",
            "jdeOrderNumber",
            "salesOrderNumber",
            "number"
        };

        public static string? TryRead(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                return ReadElement(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? ReadElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (string name in CandidateNames)
                {
                    if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                    {
                        string? text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        string? nested = ReadElement(property.Value);
                        if (!string.IsNullOrWhiteSpace(nested))
                        {
                            return nested;
                        }
                    }
                }
            }

            return null;
        }

        public static string? TryReadToken(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;
                string[] names = { "access_token", "accessToken", "token", "jwt", "id_token" };
                foreach (string name in names)
                {
                    if (root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                    {
                        string? text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }
    }
}
