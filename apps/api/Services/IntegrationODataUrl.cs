using SilaMe.Api.DTOs;

namespace SilaMe.Api.Services;

public static class IntegrationODataUrl
{
    public static string Combine(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(right)) return (left ?? string.Empty).TrimEnd('/');
        var path = right.Trim();
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
            return path.TrimEnd('/');
        var root = (left ?? string.Empty).TrimEnd('/');
        var relative = path.Trim('/');
        if (string.IsNullOrEmpty(relative)) return root;
        var extra = RemainingPath(root, relative);
        return string.IsNullOrEmpty(extra) ? root : $"{root}/{extra}";
    }

    private static string RemainingPath(string root, string relative)
    {
        var rootPath = Uri.TryCreate(root, UriKind.Absolute, out var uri) ? uri.AbsolutePath.Trim('/') : root.Trim('/');
        var rootSegments = rootPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var relativeSegments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (relativeSegments.Length == 0) return string.Empty;
        if (StartsWith(relativeSegments, rootSegments))
            return string.Join('/', relativeSegments.Skip(rootSegments.Length));
        if (EndsWith(rootSegments, relativeSegments))
            return string.Empty;
        return relative;
    }

    private static bool StartsWith(string[] left, string[] prefix) =>
        prefix.Length == 0 || (left.Length >= prefix.Length && prefix.Select((item, index) => left[index].Equals(item, StringComparison.OrdinalIgnoreCase)).All(item => item));

    private static bool EndsWith(string[] left, string[] suffix) =>
        suffix.Length == 0 || (left.Length >= suffix.Length && suffix.Select((item, index) => left[left.Length - suffix.Length + index].Equals(item, StringComparison.OrdinalIgnoreCase)).All(item => item));

    public static string WithTrailingSlash(string url) =>
        string.IsNullOrWhiteSpace(url) || url.EndsWith('/') ? url : url + "/";

    public static string ServiceRoot(string baseUrl, string? servicePath, string? entitySet = null)
    {
        var combined = Combine(baseUrl, servicePath ?? string.Empty);
        combined = StripTrailingSegment(combined, "$metadata");
        combined = StripTrailingSegment(combined, entitySet);
        return WithTrailingSlash(combined);
    }

    public static string MetadataUrl(string baseUrl, string? servicePath, string? entitySet = null) =>
        Combine(ServiceRoot(baseUrl, servicePath, entitySet).TrimEnd('/'), "$metadata");

    public static string CsrfFetchUrl(IntegrationDesignerDraft draft) =>
        CsrfFetchUrl(draft.BaseUrl, draft.ServicePath, draft.Designer.CsrfFetchPath, draft.EntitySet);

    public static string CsrfFetchUrl(string baseUrl, string? servicePath, string? csrfFetchPath, string? entitySet)
    {
        var fetch = csrfFetchPath?.Trim();
        if (string.IsNullOrWhiteSpace(fetch) || fetch is "/" or "." || LooksLikeMetadata(fetch) || LooksLikeEntitySet(fetch, entitySet))
            return ServiceRoot(baseUrl, servicePath, entitySet);
        var combined = Combine(baseUrl, fetch);
        combined = StripTrailingSegment(combined, "$metadata");
        combined = StripTrailingSegment(combined, entitySet);
        return WithTrailingSlash(combined);
    }

    private static string StripTrailingSegment(string url, string? segment)
    {
        if (string.IsNullOrWhiteSpace(segment)) return url.TrimEnd('/');
        var trimmed = url.TrimEnd('/');
        var token = segment.Trim('/');
        if (token.Length == 0) return trimmed;
        return trimmed.EndsWith("/" + token, StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^token.Length].TrimEnd('/')
            : trimmed;
    }

    private static bool LooksLikeMetadata(string path) =>
        path.Contains("$metadata", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeEntitySet(string path, string? entitySet)
    {
        if (string.IsNullOrWhiteSpace(entitySet)) return false;
        var trimmed = path.Trim('/');
        return trimmed.Equals(entitySet.Trim('/'), StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("/" + entitySet.Trim('/'), StringComparison.OrdinalIgnoreCase);
    }
}
