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

namespace Operations.Application.Features.Queries.GetStorageConnections
{
    public class GetStorageConnectionsQueryHandler : IRequestHandler<GetStorageConnectionsQuery, List<StorageConnectionResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetStorageConnectionsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<StorageConnectionResponseDto>> Handle(GetStorageConnectionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching storage connections. OrganizationId: {request.OrganizationId}");

            List<DocumentStorageConnection> connections = await _repository.DocumentStorageConnection
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive)
                .OrderBy(x => x.Provider)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Storage connections fetched. Count: {connections.Count}, OrganizationId: {request.OrganizationId}");
            return connections.Select(ResponseBuilder.StorageConnection).ToList();
        }
    }
}
