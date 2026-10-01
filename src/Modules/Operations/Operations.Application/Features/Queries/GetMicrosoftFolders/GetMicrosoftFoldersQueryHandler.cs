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

namespace Operations.Application.Features.Queries.GetMicrosoftFolders
{
    public class GetMicrosoftFoldersQueryHandler : IRequestHandler<GetMicrosoftFoldersQuery, List<MicrosoftFolderResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public GetMicrosoftFoldersQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<List<MicrosoftFolderResponseDto>> Handle(GetMicrosoftFoldersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching SharePoint folders. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}");

            if (string.IsNullOrWhiteSpace(request.Request.DriveId))
            {
                throw new BadRequestCustomException("Library is required.", "Send the id of the SharePoint document library.");
            }

            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            try
            {
                string accessToken = await MicrosoftStorageWorkflow.GetAccessTokenAsync(_graph, connection, cancellationToken);
                GraphFolder parent = await _graph.ResolveFolderAsync(accessToken, request.Request.DriveId, null, request.Request.FolderPath, cancellationToken);
                List<GraphFolder> folders = await _graph.ListChildFoldersAsync(accessToken, request.Request.DriveId, parent.Id, cancellationToken);
                await _repository.SaveAsync();
                _logger.LogInfo($"SharePoint folders fetched. ConnectionId: {connection.Id}, Count: {folders.Count}");
                return folders.Select(folder => new MicrosoftFolderResponseDto { Id = folder.Id, Name = folder.Name ?? folder.Id, Path = folder.Path }).ToList();
            }
            catch (MicrosoftGraphException exception)
            {
                _logger.LogError($"SharePoint folders could not be read. ConnectionId: {connection.Id}, Code: {exception.Code}");
                throw MicrosoftStorageWorkflow.ToCustomException(exception);
            }
        }
    }
}
