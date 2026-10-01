using Microsoft.AspNetCore.DataProtection;
using Operations.Domain.Common;
using Operations.Domain.Entities;
using Operations.Domain.Enums;

namespace Operations.Application.Services.Integration
{
    public interface IIntegrationCredentialProtector
    {
        string? Protect(string? value);
        string? Unprotect(string? value);
        string State(ApiIntegrationConfiguration configuration);
    }

    /// <summary>
    /// Encrypts the ERP credentials stored on an integration configuration (ASP.NET data protection).
    /// </summary>
    public class IntegrationCredentialProtector : IIntegrationCredentialProtector
    {
        private readonly IDataProtector _protector;

        public IntegrationCredentialProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(Common.INTEGRATION_CREDENTIAL_PURPOSE);
        }

        public string? Protect(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : _protector.Protect(value);
        }

        public string? Unprotect(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : _protector.Unprotect(value);
        }

        public string State(ApiIntegrationConfiguration configuration)
        {
            if (configuration.AuthenticationType == IntegrationAuthenticationType.NONE)
            {
                return "Not required";
            }

            bool configured = !string.IsNullOrWhiteSpace(configuration.ProtectedPassword)
                || !string.IsNullOrWhiteSpace(configuration.ProtectedClientSecret)
                || !string.IsNullOrWhiteSpace(configuration.ProtectedBearerToken);
            return configured ? "Configured" : "Missing";
        }
    }
}
