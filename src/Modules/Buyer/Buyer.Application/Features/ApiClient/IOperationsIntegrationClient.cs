using Buyer.Domain.Dtos;

namespace Buyer.Application.Contracts
{
    /// <summary>
    /// The organization's API configurations live in the integration service, one configuration for
    /// every API type. This is how the Buyer service uses them.
    /// </summary>
    public interface IOperationsIntegrationClient
    {
        /// <summary>The organization's active API for the API type; Configured is false when it has none.</summary>
        Task<IntegrationApiDto> ResolveAsync(
            Guid organizationId,
            string processType,
            string? entityCode,
            CancellationToken cancellationToken = default);

        /// <summary>Sends one document to the API, once. Never throws for a failed call: the result says what happened.</summary>
        Task<IntegrationSendResultDto> SendAsync(
            Guid organizationId,
            Guid configurationId,
            string body,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken = default);

        /// <summary>The organization's stock in hand, read from its stock API now.</summary>
        Task<StockInHandResponseDto> GetStockInHandAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);
    }
}
