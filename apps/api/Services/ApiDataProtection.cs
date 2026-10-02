using Microsoft.AspNetCore.DataProtection;

namespace SilaMe.Api.Services;

public static class ApiDataProtection
{
    public static string ApplicationName(string contentRootPath)
    {
        var configured = Environment.GetEnvironmentVariable("SILA_ME_DATA_PROTECTION_APP_NAME");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var root = Path.GetFullPath(contentRootPath);
        if (File.Exists(Path.Combine(root, "SilaMe.Api.csproj")))
            return WithTrailingSlash(root);
        var project = Path.GetFullPath(Path.Combine(root, "..", "..", ".."));
        if (File.Exists(Path.Combine(project, "SilaMe.Api.csproj")))
            return WithTrailingSlash(project);
        return WithTrailingSlash(root);
    }

    public static DirectoryInfo KeyDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("SILA_ME_DATA_PROTECTION_KEYS");
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys")
            : configured;
        var directory = new DirectoryInfo(path);
        directory.Create();
        return directory;
    }

    public static void Configure(IServiceCollection services, string contentRootPath)
    {
        services.AddDataProtection()
            .SetApplicationName(ApplicationName(contentRootPath))
            .PersistKeysToFileSystem(KeyDirectory());
    }

    private static string WithTrailingSlash(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
