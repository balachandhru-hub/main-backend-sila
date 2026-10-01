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

namespace Operations.Application.Features.Queries.GetMicrosoftLibraries
{
    public class GetMicrosoftLibrariesQueryHandler : IRequestHandler<GetMicrosoftLibrariesQuery, List<MicrosoftLibraryResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public GetMicrosoftLibrariesQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<List<MicrosoftLibraryResponseDto>> Handle(GetMicrosoftLibrariesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching SharePoint libraries. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}");

            if (string.IsNullOrWhiteSpace(request.SiteId))
            {
                throw new BadRequestCustomException("Site is required.", "Send the id of the SharePoint site.");
            }

            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            try
            {
                string accessToken = await MicrosoftStorageWorkflow.GetAccessTokenAsync(_graph, connection, cancellationToken);
                List<GraphDrive> drives = await _graph.ListDrivesAsync(accessToken, request.SiteId, cancellationToken);
                await _repository.SaveAsync();
                _logger.LogInfo($"SharePoint libraries fetched. ConnectionId: {connection.Id}, Count: {drives.Count}");
                return drives.Select(drive => new MicrosoftLibraryResponseDto { Id = drive.Id, Name = drive.Name }).ToList();
            }
            catch (MicrosoftGraphException exception)
            {
                _logger.LogError($"SharePoint libraries could not be read. ConnectionId: {connection.Id}, Code: {exception.Code}");
                throw MicrosoftStorageWorkflow.ToCustomException(exception);
            }
        }
    }
}
