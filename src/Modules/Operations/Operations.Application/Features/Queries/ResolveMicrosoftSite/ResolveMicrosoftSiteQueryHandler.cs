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

namespace Operations.Application.Features.Queries.ResolveMicrosoftSite
{
    public class ResolveMicrosoftSiteQueryHandler : IRequestHandler<ResolveMicrosoftSiteQuery, MicrosoftSiteResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public ResolveMicrosoftSiteQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<MicrosoftSiteResponseDto> Handle(ResolveMicrosoftSiteQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Resolving SharePoint site. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}");

            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            try
            {
                string accessToken = await MicrosoftStorageWorkflow.GetAccessTokenAsync(_graph, connection, cancellationToken);
                GraphSite site = await _graph.ResolveSiteAsync(accessToken, request.Request.SiteUrl, cancellationToken);

                // A renewed token is stored with the connection.
                await _repository.SaveAsync();
                _logger.LogInfo($"SharePoint site resolved. ConnectionId: {connection.Id}");
                return new MicrosoftSiteResponseDto { Id = site.Id, DisplayName = site.DisplayName, WebUrl = site.WebUrl };
            }
            catch (MicrosoftGraphException exception)
            {
                _logger.LogError($"SharePoint site could not be resolved. ConnectionId: {connection.Id}, Code: {exception.Code}");
                throw MicrosoftStorageWorkflow.ToCustomException(exception);
            }
        }
    }
}
