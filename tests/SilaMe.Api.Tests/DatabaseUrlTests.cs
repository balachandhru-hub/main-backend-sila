using Npgsql;
using SilaMe.Api.Data;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class DatabaseUrlTests
{
    [Fact]
    public void Resolve_prefers_first_non_empty_value()
    {
        Assert.Equal(
            "postgresql://sila@localhost/sila_me",
            DatabaseUrl.Resolve("  ", null, "postgresql://sila@localhost/sila_me"));
    }

    [Fact]
    public void Resolve_throws_when_no_value_is_configured()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseUrl.Resolve(" ", null, ""));
    }

    [Fact]
    public void Normalize_requires_ssl_for_hosted_postgres()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            DatabaseUrl.Normalize("postgresql://user:pass@ep-example.neon.tech/neondb"));
        Assert.Equal("ep-example.neon.tech", builder.Host);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Normalize_honors_sslmode_query()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            DatabaseUrl.Normalize("postgresql://user:pass@db.example.com:5432/app?sslmode=verify-full"));
        Assert.Equal(SslMode.VerifyFull, builder.SslMode);
    }

    [Fact]
    public void Normalize_disables_ssl_on_loopback()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            DatabaseUrl.Normalize("postgresql://sila@127.0.0.1:5432/sila_me"));
        Assert.Equal(SslMode.Disable, builder.SslMode);
    }
}
