using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Graph;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Steps shared by the Microsoft SharePoint storage commands and the transfer worker:
    /// finding the organization's connection, getting a usable access token (refreshing an
    /// expired one) and translating Graph failures into the solution's exceptions.
    /// </summary>
    internal static class MicrosoftStorageWorkflow
    {
        public static async Task<DocumentStorageConnection> GetConnectionAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid connectionId,
            Guid organizationId)
        {
            DocumentStorageConnection? connection = await repository.DocumentStorageConnection.FindFirstByConditionAsync(x =>
                x.Id == connectionId && x.OrganizationId == organizationId && x.Provider == DocumentStorageProvider.MICROSOFT);
            if (connection == null)
            {
                logger.LogError($"Microsoft connection not found. ConnectionId: {connectionId}, OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Microsoft connection not found.", "The storage connection does not exist in your organization.");
            }

            return connection;
        }

        /// <summary>
        /// Returns a valid access token of the connection. An expired token is renewed with the
        /// refresh token and stored on the (tracked) connection; the caller saves.
        /// </summary>
        public static async Task<string> GetAccessTokenAsync(IMicrosoftGraphClient graph, DocumentStorageConnection connection, CancellationToken cancellationToken)
        {
            MicrosoftToken token = graph.UnprotectToken(connection.CredentialReference);
            if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                return token.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                throw new MicrosoftGraphException("TOKEN_EXPIRED", "Microsoft authorization has expired.");
            }

            MicrosoftToken renewed = await graph.RefreshTokenAsync(token.RefreshToken, cancellationToken);
            connection.CredentialReference = graph.ProtectToken(renewed);
            return renewed.AccessToken;
        }

        public static bool IsAuthenticationFailure(MicrosoftGraphException exception)
        {
            return exception.Code is "CREDENTIAL_NOT_FOUND" or "CREDENTIAL_INVALID" or "GRAPH_FORBIDDEN" or "TOKEN_EXPIRED" or "TOKEN_EXCHANGE_FAILED";
        }

        public static BaseCustomException ToCustomException(MicrosoftGraphException exception)
        {
            string description = $"Code: {exception.Code}";
            return exception.Code switch
            {
                "SITE_URL_INVALID" or "LIBRARY_NOT_FOUND" or "GRAPH_RESOURCE_NOT_FOUND" => new BadRequestCustomException(exception.Message, description),
                "MICROSOFT_CONFIGURATION_REQUIRED" => new PreConditionFailedCustomException(exception.Message, description),
                _ => new FailedDependencyCustomException(exception.Message, description),
            };
        }

        public static MicrosoftConnectionValidationResponseDto ToValidationResponse(DocumentStorageConnection connection, string message)
        {
            return new MicrosoftConnectionValidationResponseDto
            {
                ConnectionId = connection.Id,
                Status = connection.ConnectionStatus,
                TenantId = connection.TenantIdentifier,
                SiteId = connection.SiteIdentifier,
                SiteDisplayName = connection.DisplayName,
                SiteWebUrl = connection.DisplayUrl,
                DriveId = connection.DriveIdentifier,
                DriveName = connection.DriveName,
                FolderId = connection.FolderIdentifier,
                FolderPath = connection.FolderPath,
                ValidatedAt = connection.ValidatedAt,
                Message = message
            };
        }

        /// <summary>
        /// Creates or refreshes the organization-level invoice destination of a validated connection.
        /// </summary>
        public static async Task EnsureOrganizationDestinationAsync(IRepositoryWrapper repository, DocumentStorageConnection connection, CancellationToken cancellationToken)
        {
            if (connection.ConnectionStatus != StorageConnectionStatus.CONNECTED
                || string.IsNullOrWhiteSpace(connection.FolderPath)
                || string.IsNullOrWhiteSpace(connection.SiteIdentifier)
                || string.IsNullOrWhiteSpace(connection.DriveIdentifier)
                || string.IsNullOrWhiteSpace(connection.FolderIdentifier))
            {
                return;
            }

            DocumentStorageDestination? destination = await repository.DocumentStorageDestination.FindFirstByConditionAsync(x =>
                x.OrganizationId == connection.OrganizationId
                && x.StorageConnectionId == connection.Id
                && x.DocumentType == DocumentType.INVOICE
                && x.OperatingUnitId == null);
            if (destination == null)
            {
                destination = new DocumentStorageDestination
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = connection.OrganizationId,
                    StorageConnectionId = connection.Id,
                    Provider = connection.Provider,
                    DocumentType = DocumentType.INVOICE
                };
                repository.DocumentStorageDestination.Create(destination);
            }

            destination.SiteIdentifier = connection.SiteIdentifier;
            destination.DriveIdentifier = connection.DriveIdentifier;
            destination.FolderIdentifier = connection.FolderIdentifier;
            destination.FolderPath = connection.FolderPath;
            destination.DisplayUrl = connection.DisplayUrl;
            destination.ExternalTransferEnabled = true;
            destination.Status = StorageDestinationStatus.ACTIVE;
            destination.ValidatedAt = connection.ValidatedAt;
        }
    }
}
