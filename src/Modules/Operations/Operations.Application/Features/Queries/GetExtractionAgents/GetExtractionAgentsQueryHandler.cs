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

namespace Operations.Application.Features.Queries.GetExtractionAgents
{
    public class GetExtractionAgentsQueryHandler : IRequestHandler<GetExtractionAgentsQuery, List<ExtractionAgentConfigResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetExtractionAgentsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<ExtractionAgentConfigResponseDto>> Handle(GetExtractionAgentsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching extraction agents. OrganizationId: {request.OrganizationId}");

            List<ExtractionAgentConfig> configurations = await _repository.ExtractionAgentConfig
                .FindByCondition(x => (x.OrganizationId == request.OrganizationId || x.OrganizationId == null) && x.IsActive)
                .OrderBy(x => x.Priority)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Extraction agents fetched. Count: {configurations.Count}, OrganizationId: {request.OrganizationId}");
            return configurations.Select(ResponseBuilder.ExtractionAgent).ToList();
        }
    }
}
