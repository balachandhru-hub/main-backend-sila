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

namespace Operations.Application.Features.Commands.CreateStorageConnection
{
    public class CreateStorageConnectionCommandHandler : IRequestHandler<CreateStorageConnectionCommand, StorageConnectionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateStorageConnectionCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<StorageConnectionResponseDto> Handle(CreateStorageConnectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating storage connection. Provider: {request.Request.Provider}, Name: {request.Request.Name}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            CreateStorageConnectionRequestDto dto = request.Request;
            if (dto.Provider == DocumentStorageProvider.NONE)
            {
                _logger.LogError($"Storage provider is missing. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Storage provider is required.", "Select an external storage provider.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BadRequestCustomException("Connection name is required.", "Enter a name for the storage connection.");
            }

            string name = dto.Name.Trim();
            bool exists = await _repository.DocumentStorageConnection
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.Provider == dto.Provider && x.Name == name)
                .AnyAsync(cancellationToken);
            if (exists)
            {
                _logger.LogError($"Storage connection already exists. Name: {name}, Provider: {dto.Provider}, OrganizationId: {request.OrganizationId}");
                throw new ConflictCustomException("Storage connection already exists.", "A storage connection with this name already exists.");
            }

            DocumentStorageConnection connection = new DocumentStorageConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Provider = dto.Provider,
                Name = name,
                ConnectionStatus = dto.Provider == DocumentStorageProvider.MICROSOFT
                    ? StorageConnectionStatus.AUTHENTICATION_REQUIRED
                    : StorageConnectionStatus.PENDING,
                TenantIdentifier = dto.TenantIdentifier?.Trim(),
                SiteIdentifier = dto.SiteIdentifier?.Trim(),
                DriveIdentifier = dto.DriveIdentifier?.Trim(),
                FolderIdentifier = dto.FolderIdentifier?.Trim(),
                DisplayUrl = dto.DisplayUrl?.Trim()
            };
            _repository.DocumentStorageConnection.Create(connection);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "DOCUMENT_STORAGE_CONNECTION_CREATED", "DocumentStorageConnection", connection.Id, dto.Provider.ToString(), "PENDING_CONFIGURATION");
            await _repository.SaveAsync();

            _logger.LogInfo($"Storage connection created. ConnectionId: {connection.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.StorageConnection(connection);
        }
    }
}
