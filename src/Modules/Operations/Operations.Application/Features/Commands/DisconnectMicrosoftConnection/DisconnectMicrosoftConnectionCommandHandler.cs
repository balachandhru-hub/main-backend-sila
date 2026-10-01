using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.DisconnectMicrosoftConnection
{
    public class DisconnectMicrosoftConnectionCommandHandler : IRequestHandler<DisconnectMicrosoftConnectionCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DisconnectMicrosoftConnectionCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DisconnectMicrosoftConnectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Disconnecting Microsoft connection. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            connection.ConnectionStatus = StorageConnectionStatus.DISCONNECTED;
            connection.CredentialReference = null;
            connection.SiteIdentifier = null;
            connection.DriveIdentifier = null;
            connection.FolderIdentifier = null;
            connection.FolderPath = null;
            connection.ValidatedAt = null;
            connection.LastTestedAt = DateTime.UtcNow;
            connection.LastTestStatus = "DISCONNECTED";

            List<DocumentStorageDestination> destinations = await _repository.DocumentStorageDestination
                .FindByCondition(x => x.StorageConnectionId == connection.Id)
                .ToListAsync(cancellationToken);
            foreach (DocumentStorageDestination destination in destinations)
            {
                destination.Status = StorageDestinationStatus.VALIDATION_REQUIRED;
                destination.ExternalTransferEnabled = false;
            }

            _repository.DocumentStorageDestination.UpdateRange(destinations);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "DOCUMENT_STORAGE_DISCONNECTED", "DocumentStorageConnection", connection.Id, connection.Name, "SUCCESS");
            await _repository.SaveAsync();

            _logger.LogInfo($"Microsoft connection disconnected. ConnectionId: {connection.Id}, Destinations: {destinations.Count}");
            return connection.Id;
        }
    }
}
