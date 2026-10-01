using System.Text.Json.Serialization;

namespace Operations.Application.Services.Graph
{
    /// <summary>Raised by the Microsoft Graph client. Handlers translate it into the solution's exceptions.</summary>
    public class MicrosoftGraphException : Exception
    {
        public MicrosoftGraphException(string code, string message) : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }

    /// <summary>OAuth token of a storage connection. Stored encrypted in DocumentStorageConnection.CredentialReference.</summary>
    public class MicrosoftToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class GraphSite
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = string.Empty;
    }

    public class GraphDrive
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class GraphParentReference
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }

    public class GraphFolder
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("parentReference")]
        public GraphParentReference? ParentReference { get; set; }

        [JsonPropertyName("folder")]
        public System.Text.Json.JsonElement? Folder { get; set; }

        [JsonIgnore]
        public string Path => ParentReference?.Path != null && Name != null
            ? $"{ParentReference.Path.TrimStart('/')}/{Name}"
            : Name ?? "/";
    }

    public class GraphDriveItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }

    public class GraphCollection<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new();
    }

    public class MicrosoftTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
