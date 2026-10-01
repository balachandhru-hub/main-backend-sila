using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Graph;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.ValidateMicrosoftConnection
{
    public class ValidateMicrosoftConnectionCommandHandler : IRequestHandler<ValidateMicrosoftConnectionCommand, MicrosoftConnectionValidationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public ValidateMicrosoftConnectionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<MicrosoftConnectionValidationResponseDto> Handle(ValidateMicrosoftConnectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Validating Microsoft connection. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            MicrosoftValidateConnectionRequestDto dto = request.Request;
            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            DateTime now = DateTime.UtcNow;
            try
            {
                string accessToken = await MicrosoftStorageWorkflow.GetAccessTokenAsync(_graph, connection, cancellationToken);
                GraphSite site = await _graph.ResolveSiteAsync(accessToken, dto.SiteUrl, cancellationToken);
                GraphDrive drive = await _graph.ResolveDriveAsync(accessToken, site.Id, dto.DriveId, dto.DriveName, cancellationToken);
                GraphFolder folder = await _graph.ResolveFolderAsync(accessToken, drive.Id, dto.FolderId, dto.FolderPath, cancellationToken);
                await _graph.TestWriteAsync(accessToken, drive.Id, folder.Id, cancellationToken);

                connection.ConnectionStatus = StorageConnectionStatus.CONNECTED;
                connection.SiteIdentifier = site.Id;
                connection.DisplayName = site.DisplayName;
                connection.DisplayUrl = site.WebUrl;
                connection.DriveIdentifier = drive.Id;
                connection.DriveName = drive.Name;
                connection.FolderIdentifier = folder.Id;
                connection.FolderPath = folder.Path;
                connection.ValidatedAt = now;
                connection.ValidatedByUserId = request.UserId;
                connection.LastTestedAt = now;
                connection.LastTestStatus = "PASSED";
                await MicrosoftStorageWorkflow.EnsureOrganizationDestinationAsync(_repository, connection, cancellationToken);
                AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "DOCUMENT_STORAGE_VALIDATED", "DocumentStorageConnection", connection.Id, connection.FolderPath, "SUCCESS");
                await _repository.SaveAsync();
            }
            catch (Exception exception) when (exception is MicrosoftGraphException or HttpRequestException or JsonException)
            {
                MicrosoftGraphException failure = exception as MicrosoftGraphException
                    ?? new MicrosoftGraphException("GRAPH_VALIDATION_FAILED", "Microsoft Graph could not validate the selected SharePoint destination.");
                connection.ConnectionStatus = StorageConnectionStatus.VALIDATION_FAILED;
                connection.LastTestedAt = now;
                connection.LastTestStatus = failure.Message.Length > 100 ? failure.Message[..100] : failure.Message;
                await _repository.SaveAsync();
                _logger.LogError($"Microsoft connection validation failed. ConnectionId: {connection.Id}, Code: {failure.Code}, Error: {exception.Message}");
                throw MicrosoftStorageWorkflow.ToCustomException(failure);
            }

            _logger.LogInfo($"Microsoft connection validated. ConnectionId: {connection.Id}, OrganizationId: {request.OrganizationId}");
            return MicrosoftStorageWorkflow.ToValidationResponse(connection, "Microsoft SharePoint read/write access validated.");
        }
    }
}
