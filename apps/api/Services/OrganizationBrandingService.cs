using Microsoft.AspNetCore.Http;

namespace SilaMe.Api.Services;

public sealed class OrganizationBrandingService(IHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/svg+xml",
    };

    private readonly string root = Path.Combine(environment.ContentRootPath, "storage", "branding");

    public static string? LogoUrlFor(Guid organizationId, string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? null : $"/api/v1/access/organizations/{organizationId:D}/logo";

    public async Task<(string FileName, string ContentType)> StoreAsync(
        Guid organizationId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The logo file is empty.");
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            throw new InvalidOperationException("Customer logos must be 2 MB or smaller.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType.Trim();
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Upload a PNG, JPEG, WEBP, or SVG logo.");
        }

        Directory.CreateDirectory(root);
        var extension = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".jpg",
        };
        var fileName = $"{organizationId:N}{extension}";
        var path = Path.Combine(root, fileName);
        await using (var output = File.Create(path))
        {
            await file.CopyToAsync(output, cancellationToken);
        }

        return (fileName, contentType == "image/jpg" ? "image/jpeg" : contentType);
    }

    public Task<Stream> OpenAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(root, safeName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The customer logo was not found.", path);
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public void Delete(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var path = Path.Combine(root, Path.GetFileName(fileName));
        if (File.Exists(path)) File.Delete(path);
    }
}
