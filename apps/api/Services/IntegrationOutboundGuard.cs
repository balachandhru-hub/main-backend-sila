using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace SilaMe.Api.Services;

public static class IntegrationOutboundGuard
{
    private static readonly string[] BlockedHosts =
    [
        "localhost", "metadata.google.internal", "metadata.google.com",
        "instance-data", "kubernetes.default.svc"
    ];

    public static Uri EnsureSafe(string? value, IHostEnvironment environment, string field = "URL")
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw new IntegrationException("INVALID_CONFIGURATION", $"{field} must be an absolute URL.");
        if (uri.Scheme is not "https" and not "http")
            throw new IntegrationException("UNSAFE_TARGET", $"{field} must use HTTPS.");
        var allowLoopbackHttp = environment.IsDevelopment();
        if (uri.Scheme == "http" && !allowLoopbackHttp)
            throw new IntegrationException("UNSAFE_TARGET", $"{field} must use HTTPS.");

        var host = uri.Host.Trim().Trim('[', ']');
        if (BlockedHosts.Contains(host, StringComparer.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || host.Equals("metadata", StringComparison.OrdinalIgnoreCase))
        {
            if (!(allowLoopbackHttp && IsLoopbackHost(host)))
                throw new IntegrationException("UNSAFE_TARGET", $"{field} is not an allowed outbound target.");
        }

        if (IPAddress.TryParse(host, out var ip) && !IsAllowedIp(ip, allowLoopbackHttp))
            throw new IntegrationException("UNSAFE_TARGET", $"{field} is not an allowed outbound target.");
        if (!IPAddress.TryParse(host, out _) && IsLoopbackHost(host) && !allowLoopbackHttp)
            throw new IntegrationException("UNSAFE_TARGET", $"{field} is not an allowed outbound target.");
        return uri;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedIp(IPAddress ip, bool allowLoopbackHttp)
    {
        if (IPAddress.IsLoopback(ip)) return allowLoopbackHttp;
        if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast) return false;
        if (ip.AddressFamily != AddressFamily.InterNetwork) return true;
        var bytes = ip.GetAddressBytes();
        if (bytes[0] == 10) return false;
        if (bytes[0] == 127) return allowLoopbackHttp;
        if (bytes[0] == 169 && bytes[1] == 254) return false;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
        if (bytes[0] == 192 && bytes[1] == 168) return false;
        return true;
    }
}
