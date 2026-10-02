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
            _logger.LogInfo($"Fetching integration target fields. OrganizationId: {request.OrganizationId}, ProcessType: {request.ProcessType}");

            List<IntegrationTargetFieldResponseDto> fields = request.ProcessType == null
                ? IntegrationTargetFieldRegistry.Fields
                : IntegrationTargetFieldRegistry.Fields
                    .Where(field => IntegrationProcessCatalog.OwnsTarget(request.ProcessType.Value, field.TargetField))
                    .ToList();

            _logger.LogInfo($"Integration target fields fetched. Count: {fields.Count}");
            return await Task.FromResult(fields);
        }
    }
}
