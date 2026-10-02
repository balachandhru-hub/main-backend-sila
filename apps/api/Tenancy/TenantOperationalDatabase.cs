using Npgsql;
using SilaMe.Api.Data;
using SilaMe.Api.Models;

namespace SilaMe.Api.Tenancy;

public static class TenantOperationalDatabase
{
    public const string PlatformDatabaseName = "sila_platform";
    public const string LegacyOperationalDatabaseName = "sila_me";
    public const string FiveTenantCode = "five";
    public const string FiveCustomerRouteSlug = "five";
    public const string FiveLegacyTestRouteAlias = "five-test";
    public const string FiveCustomerName = "Five Hotels and Resorts";

    public static string CloudOrigin()
    {
        var origin = Environment.GetEnvironmentVariable("SILA_ME_CLOUD_ORIGIN");
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return origin.Trim().TrimEnd('/');
        }

        var port = Environment.GetEnvironmentVariable("SILA_ME_CLOUD_PORT");
        if (string.IsNullOrWhiteSpace(port))
        {
            port = "5173";
        }

        return $"http://localhost:{port}";
    }

    public static string FiveCustomerBaseUrl() => CloudOrigin() + "/" + FiveCustomerRouteSlug;

    public static bool IsFiveOperationalSecret(string secretReference) =>
        string.Equals(secretReference, SecretReference(FiveTenantCode, TenantEnvironmentType.TEST), StringComparison.OrdinalIgnoreCase)
        || string.Equals(secretReference, SecretReference(FiveTenantCode, TenantEnvironmentType.PRODUCTION), StringComparison.OrdinalIgnoreCase);

    public static string EnvironmentCode(TenantEnvironmentType type) => type switch
    {
        TenantEnvironmentType.PRODUCTION => "PROD",
        TenantEnvironmentType.TEST => "TEST",
        TenantEnvironmentType.DEVELOPMENT => "DEV",
        _ => type.ToString().ToUpperInvariant(),
    };

    public static string Name(string tenantCode, TenantEnvironmentType type)
    {
        var code = NormalizeTenantCode(tenantCode);
        var suffix = type switch
        {
            TenantEnvironmentType.PRODUCTION => "prod",
            TenantEnvironmentType.TEST => "test",
            TenantEnvironmentType.DEVELOPMENT => "dev",
            _ => type.ToString().ToLowerInvariant(),
        };
        return $"sila_{code}_{suffix}";
    }

    public static string SecretReference(string tenantCode, TenantEnvironmentType type)
    {
        var code = NormalizeTenantCode(tenantCode).Replace('_', '-');
        var suffix = type switch
        {
            TenantEnvironmentType.PRODUCTION => "prod",
            TenantEnvironmentType.TEST => "test",
            TenantEnvironmentType.DEVELOPMENT => "dev",
            _ => type.ToString().ToLowerInvariant(),
        };
        return $"{code}-{suffix}";
    }

    public static string SecretEnvironmentVariable(string secretReference) =>
        "SILA_SECRET_" + secretReference.Replace('-', '_').ToUpperInvariant();

    public static string NormalizeTenantCode(string tenantCode) =>
        tenantCode.Trim().ToLowerInvariant().Replace('-', '_');

    public static string Bind(string operationalConnectionString, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(operationalConnectionString))
        {
            Database = databaseName,
        };
        return builder.ConnectionString;
    }

    public static async Task EnsureDatabaseExistsAsync(string operationalConnectionString, string databaseName, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(operationalConnectionString)) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        if (await exists.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        var safe = databaseName.Replace("\"", string.Empty);
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{safe}\"", connection);
        await create.ExecuteNonQueryAsync(cancellationToken);
    }
}
