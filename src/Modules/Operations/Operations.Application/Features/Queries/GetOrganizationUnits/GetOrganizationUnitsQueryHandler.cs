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

namespace Operations.Application.Features.Queries.GetOrganizationUnits
{
    public class GetOrganizationUnitsQueryHandler : IRequestHandler<GetOrganizationUnitsQuery, List<OrganizationUnitResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOrganizationUnitsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<OrganizationUnitResponseDto>> Handle(GetOrganizationUnitsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching organization units. OrganizationId: {request.OrganizationId}");

            List<OrganizationUnit> units = await _repository.OrganizationUnit
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Organization units fetched. Count: {units.Count}, OrganizationId: {request.OrganizationId}");
            return units.Select(ResponseBuilder.Unit).ToList();
        }
    }
}
