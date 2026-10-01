using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Validation and field mapping of an extraction agent, shared by the create and update commands.
    /// </summary>
    internal static class ExtractionAgentRules
    {
        public static void Apply(ExtractionAgentConfig configuration, UpsertExtractionAgentConfigRequestDto dto, ILoggerManager logger)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ProviderType))
            {
                logger.LogError($"Extraction agent name or provider type is missing. AgentId: {configuration.Id}");
                throw new BadRequestCustomException("Name and provider type are required.", "Enter the name and the provider type of the extraction agent.");
            }

            if (!string.Equals(dto.DocumentType, DocumentType.INVOICE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"Unsupported document type for an extraction agent. DocumentType: {dto.DocumentType}");
                throw new BadRequestCustomException("Unsupported document type.", "Only INVOICE extraction configuration is enabled.");
            }

            string? credentialReference = string.IsNullOrWhiteSpace(dto.CredentialReference) ? null : dto.CredentialReference.Trim();
            if (credentialReference != null && !credentialReference.StartsWith("secret://", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"Extraction agent credential is not a secret reference. AgentId: {configuration.Id}");
                throw new BadRequestCustomException("Credential reference is required.", "Store a secret:// reference, never a raw provider credential.");
            }

            configuration.Name = dto.Name.Trim();
            configuration.DocumentType = DocumentType.INVOICE.ToString();
            configuration.ProviderType = dto.ProviderType.Trim().ToUpperInvariant();
            configuration.EndpointUrl = dto.EndpointUrl?.Trim();
            configuration.AuthenticationType = dto.AuthenticationType;
            configuration.CredentialReference = credentialReference;
            configuration.CredentialLast4 = credentialReference == null
                ? null
                : credentialReference.Length <= 4 ? credentialReference : credentialReference[^4..];
            configuration.ConfigurationJson = dto.ConfigurationJson;
            configuration.Priority = dto.Priority;
            configuration.Enabled = dto.IsActive;
        }
    }
}
