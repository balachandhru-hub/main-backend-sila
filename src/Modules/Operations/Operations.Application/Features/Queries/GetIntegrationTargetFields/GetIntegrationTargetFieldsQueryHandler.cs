using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetIntegrationTargetFields
{
    public class GetIntegrationTargetFieldsQueryHandler : IRequestHandler<GetIntegrationTargetFieldsQuery, List<IntegrationTargetFieldResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetIntegrationTargetFieldsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<IntegrationTargetFieldResponseDto>> Handle(GetIntegrationTargetFieldsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integration target fields. OrganizationId: {request.OrganizationId}");
            return await Task.FromResult(IntegrationTargetFieldRegistry.Fields);
        }
    }
}
